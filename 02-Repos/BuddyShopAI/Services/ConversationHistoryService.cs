using Azure.Data.Tables;
using BuddyShopAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Polly;
using Polly.Retry;

namespace BuddyShopAI.Services;

public class ConversationHistoryService
{
    private readonly TableClient _tableClient;
    private readonly TableClient _pendingTableClient;
    private readonly ILogger<ConversationHistoryService> _logger;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly int _maxHistoryMessages = 10; // 保留最近 10 則訊息（5 輪對話）
    private readonly TimeSpan _conversationTimeout = TimeSpan.FromHours(24); // 24小時對話逾時
    private readonly TimeSpan _messageGroupingWindow = TimeSpan.FromSeconds(3); // 3秒內的訊息會被合併
    private readonly Dictionary<string, List<DateTime>> _rateLimitTracker = new(); // 請求統計追蹤

    public ConversationHistoryService(
        string connectionString,
        ILogger<ConversationHistoryService> logger)
    {
        _logger = logger;
        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient("ConversationHistory");
        _tableClient.CreateIfNotExists();
        _pendingTableClient = serviceClient.GetTableClient("PendingMessages");
        _pendingTableClient.CreateIfNotExists();
        
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("Retrying Table Storage operation. Attempt: {Attempt}, Delay: {Delay}ms", 
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<ChatHistory> GetChatHistoryAsync(string userId, string systemPrompt)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);

        try
        {
            var messages = new List<ConversationMessageEntity>();
            
            await foreach (var msg in _tableClient.QueryAsync<ConversationMessageEntity>(
                filter: $"PartitionKey eq '{userId}'",
                maxPerPage: _maxHistoryMessages * 2)) // user + assistant pairs
            {
                messages.Add(msg);
            }

            if (messages.Count > 0)
            {
                var lastMessage = messages.OrderByDescending(m => m.RowKey).First();
                if (DateTime.TryParseExact(lastMessage.RowKey, "yyyyMMddHHmmssfff", null, System.Globalization.DateTimeStyles.None, out var lastTime)
                    && DateTime.UtcNow - lastTime > _conversationTimeout)
                {
                    _logger.LogInformation("Conversation timeout for user {UserId}, clearing history", userId);
                    foreach (var msg in messages)
                    {
                        await _tableClient.DeleteEntityAsync(msg.PartitionKey, msg.RowKey);
                    }
                    return history; // 返回只有系統提示的空歷史
                }
            }

            messages = messages.OrderBy(m => m.RowKey).ToList();

            // Cap to last N messages to prevent context overflow
            if (messages.Count > _maxHistoryMessages * 2)
            {
                messages = messages.Skip(messages.Count - _maxHistoryMessages * 2).ToList();
            }

            foreach (var msg in messages)
            {
                if (msg.Role == "user")
                    history.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant")
                    history.AddAssistantMessage(msg.Content);
            }

            _logger.LogInformation("Loaded {Count} messages for user {UserId}", messages.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load conversation history for user {UserId}", userId);
        }

        return history;
    }

    public int GetHourlyRequestCount(string userId)
    {
        lock (_rateLimitTracker)
        {
            var now = DateTime.UtcNow;
            var oneHourAgo = now.AddHours(-1);

            if (!_rateLimitTracker.ContainsKey(userId))
            {
                _rateLimitTracker[userId] = new List<DateTime>();
            }

            _rateLimitTracker[userId].RemoveAll(t => t < oneHourAgo);

            _rateLimitTracker[userId].Add(now);
            
            var count = _rateLimitTracker[userId].Count;
            _logger.LogInformation("User {UserId} request count: {Count} in the last hour", userId, count);
            
            return count;
        }
    }

    public async Task SaveMessageAsync(string userId, string role, string content)
    {
        try
        {
            await _retryPipeline.ExecuteAsync(async ct =>
            {
                var entity = new ConversationMessageEntity
                {
                    PartitionKey = userId,
                    RowKey = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
                    Role = role,
                    Content = content
                };

                await _tableClient.AddEntityAsync(entity, ct);
                _logger.LogInformation("Saved {Role} message for user {UserId}", role, userId);
            }, CancellationToken.None);

            _ = CleanupOldMessagesAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {Role} message for user {UserId} after retries", role, userId);
        }
    }

    public async Task<string> BufferPendingMessageAsync(string userId, string content, string replyToken)
    {
        var rowKey = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        try
        {
            await _retryPipeline.ExecuteAsync(async ct =>
            {
                var entity = new PendingMessageEntity
                {
                    PartitionKey = userId,
                    RowKey = rowKey,
                    Content = content,
                    ReplyToken = replyToken
                };

                await _pendingTableClient.AddEntityAsync(entity, ct);
                _logger.LogInformation("Buffered pending message for user {UserId}, RowKey: {RowKey}", userId, rowKey);
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to buffer pending message for user {UserId}", userId);
            throw; // 往上拋，讓 caller 決定是否直接處理
        }
        return rowKey;
    }

    public async Task<(string? combinedMessage, string? latestReplyToken)> WaitAndCollectMessagesAsync(string userId, string myRowKey)
    {
        await Task.Delay(_messageGroupingWindow);

        var pendingMessages = new List<PendingMessageEntity>();
        await foreach (var msg in _pendingTableClient.QueryAsync<PendingMessageEntity>(
            filter: $"PartitionKey eq '{userId}'"))
        {
            pendingMessages.Add(msg);
        }

        if (pendingMessages.Count == 0)
        {
            _logger.LogWarning("No pending messages found for user {UserId}, possibly already processed", userId);
            return (null, null);
        }

        pendingMessages = pendingMessages.OrderBy(m => m.RowKey).ToList();
        var latestRowKey = pendingMessages.Last().RowKey;

        if (myRowKey != latestRowKey)
        {
            _logger.LogInformation("User {UserId}: newer message exists (my: {MyRowKey}, latest: {LatestRowKey}), skipping",
                userId, myRowKey, latestRowKey);
            return (null, null);
        }

        var combinedMessage = string.Join("\n", pendingMessages.Select(m => m.Content));
        var latestReplyToken = pendingMessages.Last().ReplyToken;

        _logger.LogInformation("User {UserId}: combining {Count} messages into one",
            userId, pendingMessages.Count);

        foreach (var msg in pendingMessages)
        {
            try
            {
                await _pendingTableClient.DeleteEntityAsync(msg.PartitionKey, msg.RowKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete pending message {RowKey} for user {UserId}",
                    msg.RowKey, userId);
            }
        }

        return (combinedMessage, latestReplyToken);
    }

    public async Task<List<UserLastMessageInfo>> GetRecentUserLastMessagesAsync(int withinHours = 48)
    {
        var cutoff = DateTime.UtcNow.AddHours(-withinHours).ToString("yyyyMMddHHmmssfff");
        var allMessages = new List<ConversationMessageEntity>();

        await foreach (var msg in _tableClient.QueryAsync<ConversationMessageEntity>(
            filter: $"RowKey ge '{cutoff}'"))
        {
            allMessages.Add(msg);
        }

        var result = allMessages
            .GroupBy(m => m.PartitionKey)
            .Select(g =>
            {
                var lastMsg = g.OrderByDescending(m => m.RowKey).First();
                return new UserLastMessageInfo
                {
                    UserId = g.Key,
                    LastMessageRole = lastMsg.Role,
                    LastMessageContent = lastMsg.Content.Length > 60
                        ? lastMsg.Content[..60] + "..."
                        : lastMsg.Content,
                    LastMessageTime = DateTime.TryParseExact(lastMsg.RowKey, "yyyyMMddHHmmssfff",
                        null, System.Globalization.DateTimeStyles.None, out var t) ? t : DateTime.MinValue,
                    TotalMessages = g.Count()
                };
            })
            .OrderByDescending(u => u.LastMessageTime)
            .ToList();

        return result;
    }

    private async Task CleanupOldMessagesAsync(string userId)
    {
        try
        {
            var allMessages = new List<ConversationMessageEntity>();
            
            await foreach (var msg in _tableClient.QueryAsync<ConversationMessageEntity>(
                filter: $"PartitionKey eq '{userId}'"))
            {
                allMessages.Add(msg);
            }

            var messagesToDelete = allMessages
                .OrderByDescending(m => m.RowKey)
                .Skip(_maxHistoryMessages * 2) // Keep recent messages (user + assistant pairs)
                .ToList();

            foreach (var msg in messagesToDelete)
            {
                await _tableClient.DeleteEntityAsync(msg.PartitionKey, msg.RowKey);
                _logger.LogDebug("Deleted old message {RowKey} for user {UserId}", msg.RowKey, userId);
            }

            if (messagesToDelete.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old messages for user {UserId}", 
                    messagesToDelete.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old messages for user {UserId}", userId);
        }
    }

    public async Task<string> GetRecentMessagesPreviewAsync(string userId, int count = 3)
    {
        try
        {
            var messages = new List<ConversationMessageEntity>();
            await foreach (var msg in _tableClient.QueryAsync<ConversationMessageEntity>(
                filter: $"PartitionKey eq '{userId}'"))
            {
                messages.Add(msg);
            }

            var recent = messages
                .OrderByDescending(m => m.RowKey)
                .Take(count)
                .Reverse()
                .ToList();

            if (recent.Count == 0)
                return "（無最近對話記錄）";

            var lines = recent.Select(m =>
            {
                var role = m.Role == "user" ? "👤 客人" : "🤖 AI";
                var content = m.Content?.Length > 60 ? m.Content[..60] + "..." : m.Content;
                return $"{role}：{content}";
            });

            return string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent messages preview for {UserId}", userId);
            return "（無法讀取對話記錄）";
        }
    }
}

public class UserLastMessageInfo
{
    public string UserId { get; set; } = string.Empty;
    public string LastMessageRole { get; set; } = string.Empty;
    public string LastMessageContent { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int TotalMessages { get; set; }
}

