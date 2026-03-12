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
    private readonly UserModeService _userModeService;
    private readonly PromptProvider _promptProvider;
    private readonly TelemetryClient _telemetryClient;
    private readonly ManageCommandService _manageCommandService;

    public LineWebhook(
        ILogger<LineWebhook> logger,
        ILineMessagingClient lineClient,
        LineSignatureValidator signatureValidator,
        Kernel kernel,
        ConversationHistoryService historyService,
        UserModeService userModeService,
        PromptProvider promptProvider,
        TelemetryClient telemetryClient,
        ManageCommandService manageCommandService)
    {
        _logger = logger;
        _lineClient = lineClient;
        _signatureValidator = signatureValidator;
        _kernel = kernel;
        _historyService = historyService;
        _userModeService = userModeService;
        _promptProvider = promptProvider;
        _telemetryClient = telemetryClient;
        _manageCommandService = manageCommandService;
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

            if (!_signatureValidator.ValidateSignature(body, signature))
            {
                _logger.LogWarning("Invalid signature. OperationId: {OperationId}", operationId);
                _telemetryClient.TrackEvent("SignatureValidationFailed", 
                    new Dictionary<string, string> { ["operationId"] = operationId });
                operation.Telemetry.Success = false;
                return new UnauthorizedResult();
            }

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

                if (_manageCommandService.IsManager(userId) && _manageCommandService.IsManageCommand(textMessage.Text))
                {
                    _logger.LogInformation("Manage command intercepted from {UserId}. OperationId: {OperationId}", userId, operationId);
                    _telemetryClient.TrackEvent("ManageCommandReceived", new Dictionary<string, string>
                    {
                        ["operationId"] = operationId,
                        ["userId"] = userId
                    });
                    await _manageCommandService.HandleAsync(userId, textMessage.Text);
                    return; // 不進入一般 AI 流程
                }

                if (await _userModeService.IsHumanModeAsync(userId))
                {
                    _logger.LogInformation(
                        "User {UserId} is in HUMAN mode. Saving message without AI response. OperationId: {OperationId}",
                        userId, operationId);
                    await _historyService.SaveMessageAsync(userId, "user", textMessage.Text);
                    _telemetryClient.TrackEvent("HumanModeMessageReceived", new Dictionary<string, string>
                    {
                        ["operationId"] = operationId,
                        ["userId"] = userId
                    });
                    return; // 不進行 AI 回應，等待真人客服介入
                }

                var requestCount = _historyService.GetHourlyRequestCount(userId);
                _telemetryClient.TrackMetric("UserRequestsPerHour", requestCount, 
                    new Dictionary<string, string>
                    {
                        ["operationId"] = operationId,
                        ["userId"] = userId
                    });

                var myRowKey = await _historyService.BufferPendingMessageAsync(userId, textMessage.Text, messageEvent.ReplyToken);

                var (combinedMessage, latestReplyToken) = 
                    await _historyService.WaitAndCollectMessagesAsync(userId, myRowKey);

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

                var systemPrompt = await _promptProvider.GetSystemPromptAsync();
                var history = await _historyService.GetChatHistoryAsync(userId, systemPrompt);

                history.AddUserMessage(combinedMessage);
                
                await _historyService.SaveMessageAsync(userId, "user", combinedMessage);

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

                await _historyService.SaveMessageAsync(userId, "assistant", responseText);

                if (responseText.Contains("轉接給專人") || responseText.Contains("轉接專人") ||
                    responseText.Contains("轉接給真人") || responseText.Contains("轉接真人") ||
                    responseText.Contains("轉給專人") || responseText.Contains("由人工") ||
                    responseText.Contains("交由專人"))
                {
                    _logger.LogInformation(
                        "🔄 Auto-handoff detected for user {UserId}. Switching to human mode. OperationId: {OperationId}",
                        userId, operationId);

                    await _userModeService.SetUserModeAsync(userId, "human", "AI 自動轉接：偵測到轉接專人回覆");

                    _telemetryClient.TrackEvent("AutoHumanHandoff", new Dictionary<string, string>
                    {
                        ["operationId"] = operationId,
                        ["userId"] = userId,
                        ["trigger"] = "AI response contains handoff keyword"
                    });

                    var manageUserIds = _manageCommandService.GetManagerIds();
                    foreach (var manageUserId in manageUserIds)
                    {
                        string displayName;
                        try
                        {
                            var profile = await _lineClient.GetUserProfileAsync(userId);
                            displayName = profile.DisplayName;
                        }
                        catch
                        {
                            displayName = userId[..Math.Min(12, userId.Length)] + "...";
                        }

                        var recentMessages = await _historyService.GetRecentMessagesPreviewAsync(userId, 3);
                        var managerNotification =
                            $"🔴 自動轉接通知\n\n" +
                            $"👤 用戶名稱：{displayName}\n" +
                            $"🆔 UserId：{userId[..Math.Min(12, userId.Length)]}...\n\n" +
                            $"📝 最近對話：\n{recentMessages}\n\n" +
                            $"請至 LINE 官方帳號管理員後台查看聊天記錄\n\n" +
                            $"👇 處理完畢後點下方按鈕切回 AI：";
                        try
                        {
                            var quickReply = new QuickReply
                            {
                                Items = new[]
                                {
                                    new QuickReplyButtonObject(new MessageTemplateAction("✅ 切回AI", $"/manage ai {userId}")),
                                    new QuickReplyButtonObject(new MessageTemplateAction("📋 查看模式", "/manage modes"))
                                }
                            };
                            await _lineClient.PushMessageAsync(manageUserId, new[] { new TextMessage(managerNotification, quickReply) });
                        }
                        catch (Exception notifyEx)
                        {
                            _logger.LogWarning(notifyEx, "Failed to notify manager about auto-handoff for {UserId}", userId);
                        }
                    }
                }

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

    private async Task SendMessageWithFallbackAsync(string replyToken, string userId, string text, string operationId)
    {
        var segments = text
            .Split("[SPLIT]", StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Take(5)
            .ToArray();

        var messages = segments.Select(s => new TextMessage(s)).ToArray<ISendMessage>();

        if (segments.Length > 1)
        {
            _logger.LogInformation("Splitting reply into {Count} messages for user {UserId}. OperationId: {OperationId}",
                segments.Length, userId, operationId);
        }

        try
        {
            await _lineClient.ReplyMessageAsync(replyToken, messages);
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
                await _lineClient.PushMessageAsync(userId, messages);
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

    private async Task<ChatMessageContent> GetAIResponseWithRetryAsync(
        IChatCompletionService chatService, 
        ChatHistory history,
        string operationId,
        int maxRetries = 3)
    {
        var executionSettings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                ["temperature"] = 0,
                ["max_tokens"] = 2048
            }
        };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await chatService.GetChatMessageContentAsync(history, executionSettings);
            }
            catch (Microsoft.SemanticKernel.HttpOperationException ex) when (ex.Message.Contains("429"))
            {
                if (attempt == maxRetries - 1)
                {
                    throw; // 最後一次嘗試失敗，往上拋出
                }

                var delaySeconds = Math.Pow(2, attempt);
                _logger.LogWarning("Rate limit hit (429), retrying in {Delay}s... (Attempt {Attempt}/{MaxRetries}). OperationId: {OperationId}", 
                    delaySeconds, attempt + 1, maxRetries, operationId);
                
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        throw new InvalidOperationException("Should not reach here");
    }
}

