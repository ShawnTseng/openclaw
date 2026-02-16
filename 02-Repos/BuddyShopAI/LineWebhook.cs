using Line.Messaging;
using Line.Messaging.Webhooks;
using BuddyShopAI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace BuddyShopAI;

public class LineWebhook
{
    private readonly ILogger<LineWebhook> _logger;
    private readonly ILineMessagingClient _lineClient;
    private readonly LineSignatureValidator _signatureValidator;
    private readonly Kernel _kernel;
    private readonly ConversationHistoryService _historyService;
    private readonly PromptProvider _promptProvider;
    private readonly TelemetryClient _telemetryClient;

    public LineWebhook(
        ILogger<LineWebhook> logger,
        ILineMessagingClient lineClient,
        LineSignatureValidator signatureValidator,
        Kernel kernel,
        ConversationHistoryService historyService,
        PromptProvider promptProvider,
        TelemetryClient telemetryClient)
    {
        _logger = logger;
        _lineClient = lineClient;
        _signatureValidator = signatureValidator;
        _kernel = kernel;
        _historyService = historyService;
        _promptProvider = promptProvider;
        _telemetryClient = telemetryClient;
    }

    [Function("LineWebhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "LineWebhook")] HttpRequest req)
    {
        var operationId = Guid.NewGuid().ToString();
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>("LineWebhook");
        operation.Telemetry.Properties["operationId"] = operationId;
        
        string? body = null;
        try
        {
            // Ensure stream is at the beginning
            if (req.Body.CanSeek)
            {
                req.Body.Position = 0;
            }
            
            using var reader = new StreamReader(req.Body, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            
            if (string.IsNullOrEmpty(body))
            {
                _logger.LogWarning("Empty request body. OperationId: {OperationId}", operationId);
                operation.Telemetry.Success = false;
                return new BadRequestResult();
            }
            
            _logger.LogInformation("Received webhook request. Body length: {Length}, OperationId: {OperationId}", 
                body.Length, operationId);
            
            var signature = req.Headers["X-Line-Signature"].FirstOrDefault();

            // Validate signature
            if (!_signatureValidator.ValidateSignature(body, signature))
            {
                _logger.LogWarning("Invalid signature. OperationId: {OperationId}", operationId);
                _telemetryClient.TrackEvent("SignatureValidationFailed", 
                    new Dictionary<string, string> { ["operationId"] = operationId });
                operation.Telemetry.Success = false;
                return new UnauthorizedResult();
            }

            // Process events
            var events = WebhookEventParser.Parse(body);
            foreach (var ev in events)
            {
                await ProcessEventAsync(ev, operationId);
            }

            operation.Telemetry.Success = true;
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook error. Body length: {BodyLength}, OperationId: {OperationId}", 
                body?.Length ?? -1, operationId);
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex, new Dictionary<string, string> 
            { 
                ["operationId"] = operationId,
                ["errorLocation"] = "WebhookProcessing",
                ["bodyLength"] = (body?.Length ?? -1).ToString(),
                ["exceptionType"] = ex.GetType().Name
            });
            return new OkResult();
        }
    }

    private async Task ProcessEventAsync(WebhookEvent webhookEvent, string operationId)
    {
        try
        {
            if (webhookEvent is MessageEvent { Message: TextEventMessage textMessage } messageEvent)
            {
                var userId = messageEvent.Source.UserId;
                
                _logger.LogInformation("User {UserId}: {Message}. OperationId: {OperationId}", userId, textMessage.Text, operationId);

                // 記錄使用統計
                var requestCount = _historyService.GetHourlyRequestCount(userId);
                _telemetryClient.TrackMetric("UserRequestsPerHour", requestCount, 
                    new Dictionary<string, string>
                    {
                        ["operationId"] = operationId,
                        ["userId"] = userId
                    });

                // ── 訊息合併流程 ──
                // 1. 將訊息暫存到 Table Storage
                var myRowKey = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                await _historyService.BufferPendingMessageAsync(userId, textMessage.Text, messageEvent.ReplyToken);

                // 2. 等待 grouping window（3 秒），讓後續訊息有時間進入 buffer
                var (combinedMessage, latestReplyToken) = 
                    await _historyService.WaitAndCollectMessagesAsync(userId, myRowKey);

                // 3. 如果不是最新的 request → 跳過，由最新的 request 處理
                if (combinedMessage == null)
                {
                    _logger.LogInformation("User {UserId}: skipping, a newer request will handle the grouped messages. OperationId: {OperationId}",
                        userId, operationId);
                    return;
                }

                _telemetryClient.TrackEvent("MessageGrouped", new Dictionary<string, string>
                {
                    ["operationId"] = operationId,
                    ["userId"] = userId,
                    ["messageLength"] = combinedMessage.Length.ToString()
                });

                // ── 正常 AI 處理流程 ──
                // 取得對話歷史（含系統提示）
                var systemPrompt = await _promptProvider.GetSystemPromptAsync();
                var history = await _historyService.GetChatHistoryAsync(userId, systemPrompt);

                // 加入合併後的用戶訊息
                history.AddUserMessage(combinedMessage);
                
                // 儲存用戶訊息到歷史
                await _historyService.SaveMessageAsync(userId, "user", combinedMessage);

                // Get AI response with retry mechanism
                string responseText;
                var aiStartTime = DateTime.UtcNow;
                _telemetryClient.TrackEvent("OpenAIRequestStart", new Dictionary<string, string>
                {
                    ["operationId"] = operationId,
                    ["userId"] = userId
                });
                
                try
                {
                    var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                    var response = await GetAIResponseWithRetryAsync(chatService, history, operationId);
                    responseText = response.Content ?? "抱歉，我現在有點忙不過來，請稍後再試或聯絡真人客服。";
                    
                    var aiDuration = DateTime.UtcNow - aiStartTime;
                    _telemetryClient.TrackMetric("OpenAIResponseTime", aiDuration.TotalMilliseconds, 
                        new Dictionary<string, string>
                        {
                            ["operationId"] = operationId,
                            ["userId"] = userId,
                            ["success"] = "true"
                        });
                }
                catch (Exception aiEx)
                {
                    var aiDuration = DateTime.UtcNow - aiStartTime;
                    _logger.LogError(aiEx, "AI service failed after retries. OperationId: {OperationId}", operationId);
                    _telemetryClient.TrackException(aiEx, new Dictionary<string, string>
                    {
                        ["operationId"] = operationId,
                        ["userId"] = userId,
                        ["errorLocation"] = "OpenAIService"
                    });
                    _telemetryClient.TrackMetric("OpenAIResponseTime", aiDuration.TotalMilliseconds, 
                        new Dictionary<string, string>
                        {
                            ["operationId"] = operationId,
                            ["userId"] = userId,
                            ["success"] = "false"
                        });
                    responseText = "不好意思，目前系統有點忙碌中 😅\n您的問題我已經記錄下來，會盡快請真人小編為您處理！\n\n或者您也可以稍後再試試看喔！";
                }

                // 儲存 AI 回應到歷史
                await _historyService.SaveMessageAsync(userId, "assistant", responseText);

                // Reply to LINE user（使用最新的 ReplyToken，過期時 fallback 到 Push Message）
                await SendMessageWithFallbackAsync(latestReplyToken!, userId, responseText, operationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event processing failed. OperationId: {OperationId}", operationId);
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                ["operationId"] = operationId,
                ["errorLocation"] = "ProcessEvent"
            });

            // 嘗試通知用戶系統異常
            if (webhookEvent is MessageEvent { Source.UserId: string failedUserId })
            {
                try
                {
                    await _lineClient.PushMessageAsync(failedUserId, new[] {
                        new TextMessage("不好意思，系統暫時出了點小狀況 😅\n請稍後再試，或直接聯絡真人小編喔！")
                    });
                }
                catch (Exception pushEx)
                {
                    _logger.LogError(pushEx, "Failed to send error notification to user {UserId}", failedUserId);
                }
            }
        }
    }

    /// <summary>
    /// 回覆用戶：優先用 ReplyToken，過期或失敗時 fallback 到 Push Message API
    /// </summary>
    private async Task SendMessageWithFallbackAsync(string replyToken, string userId, string text, string operationId)
    {
        var message = new TextMessage(text);
        try
        {
            await _lineClient.ReplyMessageAsync(replyToken, new[] { message });
            _logger.LogInformation("Reply sent to user {UserId}. OperationId: {OperationId}", userId, operationId);
        }
        catch (LineResponseException ex)
        {
            _logger.LogWarning("ReplyToken expired or invalid for user {UserId}, falling back to Push Message. Error: {Error}. OperationId: {OperationId}",
                userId, ex.Message, operationId);
            _telemetryClient.TrackEvent("ReplyTokenFallbackToPush", new Dictionary<string, string>
            {
                ["operationId"] = operationId,
                ["userId"] = userId,
                ["replyError"] = ex.Message
            });

            try
            {
                await _lineClient.PushMessageAsync(userId, new[] { message });
                _logger.LogInformation("Push message sent to user {UserId}. OperationId: {OperationId}", userId, operationId);
            }
            catch (Exception pushEx)
            {
                _logger.LogError(pushEx, "Push message also failed for user {UserId}. OperationId: {OperationId}", userId, operationId);
                _telemetryClient.TrackException(pushEx, new Dictionary<string, string>
                {
                    ["operationId"] = operationId,
                    ["userId"] = userId,
                    ["errorLocation"] = "PushMessageFallback"
                });
            }
        }
    }

    /// <summary>
    /// 使用 Exponential Backoff 重試機制呼叫 AI
    /// </summary>
    private async Task<ChatMessageContent> GetAIResponseWithRetryAsync(
        IChatCompletionService chatService, 
        ChatHistory history,
        string operationId,
        int maxRetries = 3)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await chatService.GetChatMessageContentAsync(history);
            }
            catch (Microsoft.SemanticKernel.HttpOperationException ex) when (ex.Message.Contains("429"))
            {
                if (attempt == maxRetries - 1)
                {
                    throw; // 最後一次嘗試失敗，往上拋出
                }

                // Exponential backoff: 1s, 2s, 4s
                var delaySeconds = Math.Pow(2, attempt);
                _logger.LogWarning("Rate limit hit (429), retrying in {Delay}s... (Attempt {Attempt}/{MaxRetries}). OperationId: {OperationId}", 
                    delaySeconds, attempt + 1, maxRetries, operationId);
                
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        throw new InvalidOperationException("Should not reach here");
    }
}
