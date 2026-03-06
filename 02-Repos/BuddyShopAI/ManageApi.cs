using BuddyShopAI.Services;
using Line.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace BuddyShopAI;

public class ManageApi
{
    private readonly ILogger<ManageApi> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConversationHistoryService _historyService;
    private readonly UserModeService _userModeService;
    private readonly PromptProvider _promptProvider;
    private readonly Kernel _kernel;
    private readonly ILineMessagingClient _lineClient;

    private const string AppVersion = "1.3.0";

    public ManageApi(
        ILogger<ManageApi> logger,
        IConfiguration configuration,
        ConversationHistoryService historyService,
        UserModeService userModeService,
        PromptProvider promptProvider,
        Kernel kernel,
        ILineMessagingClient lineClient)
    {
        _logger = logger;
        _configuration = configuration;
        _historyService = historyService;
        _userModeService = userModeService;
        _promptProvider = promptProvider;
        _kernel = kernel;
        _lineClient = lineClient;
    }

    private bool ValidateManageKey(HttpRequest req)
    {
        var configuredKey = _configuration["Manage:ApiKey"];
        if (string.IsNullOrEmpty(configuredKey))
        {
            _logger.LogWarning("Manage:ApiKey is not configured. Admin endpoints are inaccessible.");
            return false;
        }

        var providedKey = req.Headers["X-Manage-Key"].FirstOrDefault();
        return providedKey == configuredKey;
    }

    [Function("ManageStatus")]
    public async Task<IActionResult> GetStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/status")] HttpRequest req)
    {
        if (!ValidateManageKey(req)) return new UnauthorizedResult();

        var humanModeUsers = await _userModeService.GetAllUserModesAsync();
        var humanCount = humanModeUsers.Count(u => u.Mode == "human");

        var status = new
        {
            version = AppVersion,
            status = "running",
            timestamp = DateTime.UtcNow,
            stats = new
            {
                totalUsersTracked = humanModeUsers.Count,
                usersInHumanMode = humanCount,
                usersInAiMode = humanModeUsers.Count - humanCount
            },
            config = new
            {
                tenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? "mrvshop",
                azureOpenAIEndpoint = _configuration["AzureOpenAI:Endpoint"],
                deploymentName = _configuration["AzureOpenAI:DeploymentName"],
                functionsRuntime = Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME"),
                region = Environment.GetEnvironmentVariable("REGION_NAME")
            }
        };

        _logger.LogInformation("Admin status check. Version: {Version}", AppVersion);
        return new OkObjectResult(status);
    }

    [Function("ManageRetry")]
    public async Task<IActionResult> RetryResponse(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/users/{userId}/retry")] HttpRequest req,
        string userId)
    {
        if (!ValidateManageKey(req)) return new UnauthorizedResult();

        _logger.LogInformation("Admin retry triggered for user {UserId}", userId);

        try
        {
            var systemPrompt = await _promptProvider.GetSystemPromptAsync();
            var history = await _historyService.GetChatHistoryAsync(userId, systemPrompt);

            var conversationMessages = history
                .Where(m => m.Role == AuthorRole.User || m.Role == AuthorRole.Assistant)
                .ToList();

            if (conversationMessages.Count == 0)
            {
                return new BadRequestObjectResult(new
                {
                    error = "No conversation history found for this user",
                    userId
                });
            }

            var lastMessage = conversationMessages.Last();

            if (lastMessage.Role == AuthorRole.Assistant && lastMessage.Content != null)
            {
                await _lineClient.PushMessageAsync(userId, new[] { new TextMessage(lastMessage.Content) });
                _logger.LogInformation("Admin retry: resent last AI response to user {UserId}", userId);

                return new OkObjectResult(new
                {
                    success = true,
                    action = "resent_last_response",
                    userId,
                    responseLength = lastMessage.Content.Length
                });
            }

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var aiResponse = await GetAIResponseWithRetryAsync(chatService, history, userId);
            var responseText = aiResponse.Content ?? "抱歉，我現在有點忙不過來，請稍後再試。";

            await _historyService.SaveMessageAsync(userId, "assistant", responseText);

            await _lineClient.PushMessageAsync(userId, new[] { new TextMessage(responseText) });

            _logger.LogInformation("Admin retry: AI response generated and pushed to user {UserId}", userId);
            return new OkObjectResult(new
            {
                success = true,
                action = "generated_new_response",
                userId,
                responseLength = responseText.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin retry failed for user {UserId}", userId);
            return new ObjectResult(new { error = ex.Message, userId }) { StatusCode = 500 };
        }
    }

    [Function("ManagePushMessage")]
    public async Task<IActionResult> PushMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/users/{userId}/push")] HttpRequest req,
        string userId)
    {
        if (!ValidateManageKey(req)) return new UnauthorizedResult();

        string body;
        using var reader = new StreamReader(req.Body);
        body = await reader.ReadToEndAsync();

        ManagePushMessageRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ManagePushMessageRequest>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Invalid JSON body" });
        }

        if (string.IsNullOrWhiteSpace(payload?.Message))
            return new BadRequestObjectResult(new { error = "Field 'message' is required and cannot be empty" });

        try
        {
            await _historyService.SaveMessageAsync(userId, "assistant", payload.Message);

            await _lineClient.PushMessageAsync(userId, new[] { new TextMessage(payload.Message) });

            _logger.LogInformation("Admin push message sent to user {UserId} ({Length} chars)", userId, payload.Message.Length);
            return new OkObjectResult(new { success = true, userId, messageLength = payload.Message.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin push message failed for user {UserId}", userId);
            return new ObjectResult(new { error = ex.Message, userId }) { StatusCode = 500 };
        }
    }

    [Function("ManageSetUserMode")]
    public async Task<IActionResult> SetUserMode(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/users/{userId}/mode")] HttpRequest req,
        string userId)
    {
        if (!ValidateManageKey(req)) return new UnauthorizedResult();

        string body;
        using var reader = new StreamReader(req.Body);
        body = await reader.ReadToEndAsync();

        ManageSetModeRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ManageSetModeRequest>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Invalid JSON body" });
        }

        if (payload?.Mode != "ai" && payload?.Mode != "human")
            return new BadRequestObjectResult(new { error = "Field 'mode' must be 'ai' or 'human'" });

        try
        {
            await _userModeService.SetUserModeAsync(userId, payload.Mode, payload.Note);

            _logger.LogInformation("Admin set user {UserId} to {Mode} mode. Note: {Note}",
                userId, payload.Mode, payload.Note ?? "(none)");

            return new OkObjectResult(new
            {
                success = true,
                userId,
                mode = payload.Mode,
                note = payload.Note,
                changedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set mode for user {UserId}", userId);
            return new ObjectResult(new { error = ex.Message, userId }) { StatusCode = 500 };
        }
    }

    [Function("ManageGetUserMode")]
    public async Task<IActionResult> GetUserMode(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/users/{userId}/mode")] HttpRequest req,
        string userId)
    {
        if (!ValidateManageKey(req)) return new UnauthorizedResult();

        try
        {
            var entity = await _userModeService.GetUserModeAsync(userId);
            return new OkObjectResult(new
            {
                userId,
                mode = entity.Mode,
                changedAt = entity.ChangedAt == DateTime.MinValue ? (DateTime?)null : entity.ChangedAt,
                note = entity.Note
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mode for user {UserId}", userId);
            return new ObjectResult(new { error = ex.Message, userId }) { StatusCode = 500 };
        }
    }

    [Function("ManageListUserModes")]
    public async Task<IActionResult> ListUserModes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/users/modes")] HttpRequest req)
    {
        if (!ValidateManageKey(req)) return new UnauthorizedResult();

        try
        {
            var modes = await _userModeService.GetAllUserModesAsync();
            return new OkObjectResult(new
            {
                total = modes.Count,
                humanMode = modes.Count(u => u.Mode == "human"),
                aiMode = modes.Count(u => u.Mode == "ai"),
                users = modes.Select(m => new
                {
                    userId = m.RowKey,
                    mode = m.Mode,
                    changedAt = m.ChangedAt,
                    note = m.Note
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list user modes");
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    [Function("ManageListRecentUsers")]
    public async Task<IActionResult> ListRecentUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/users/recent")] HttpRequest req)
    {
        if (!ValidateManageKey(req)) return new UnauthorizedResult();

        int hours = 48;
        if (req.Query.TryGetValue("hours", out var hoursStr) && int.TryParse(hoursStr, out var parsed))
            hours = Math.Clamp(parsed, 1, 168); // 最多 7 天

        try
        {
            var users = await _historyService.GetRecentUserLastMessagesAsync(hours);
            var unanswered = users.Where(u => u.LastMessageRole == "user").ToList();

            return new OkObjectResult(new
            {
                queryHours = hours,
                totalActiveUsers = users.Count,
                unansweredCount = unanswered.Count,
                unanswered = unanswered.Select(u => new
                {
                    userId = u.UserId,
                    lastMessage = u.LastMessageContent,
                    lastMessageTime = u.LastMessageTime,
                    totalMessages = u.TotalMessages
                }),
                allUsers = users.Select(u => new
                {
                    userId = u.UserId,
                    lastRole = u.LastMessageRole,
                    lastMessage = u.LastMessageContent,
                    lastMessageTime = u.LastMessageTime,
                    totalMessages = u.TotalMessages,
                    needsReply = u.LastMessageRole == "user"
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list recent users");
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    private async Task<ChatMessageContent> GetAIResponseWithRetryAsync(
        IChatCompletionService chatService,
        ChatHistory history,
        string userId,
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
                if (attempt == maxRetries - 1) throw;

                var delaySeconds = Math.Pow(2, attempt);
                _logger.LogWarning(
                    "Admin retry: rate limit hit (429), retrying in {Delay}s... (Attempt {Attempt}/{Max}) UserId: {UserId}",
                    delaySeconds, attempt + 1, maxRetries, userId);

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        throw new InvalidOperationException("Should not reach here");
    }
}

public class ManagePushMessageRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ManageSetModeRequest
{
    public string Mode { get; set; } = string.Empty;
    public string? Note { get; set; }
}

