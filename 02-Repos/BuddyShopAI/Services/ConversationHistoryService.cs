using Azure.Data.Tables;
using BuddyShopAI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Polly;
using Polly.Retry;

namespace BuddyShopAI.Services;

/// <summary>
/// Service for managing conversation history in Azure Table Storage
/// </summary>
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
        
        // Configure retry policy for Table Storage operations
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

    /// <summary>
    /// Get chat history for a specific user
    /// </summary>
    public async Task<ChatHistory> GetChatHistoryAsync(string userId, string systemPrompt)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);

        try
        {
            // Query messages for this user, ordered by RowKey (timestamp) descending
            var messages = new List<ConversationMessageEntity>();
            
            await foreach (var msg in _tableClient.QueryAsync<ConversationMessageEntity>(
                filter: $"PartitionKey eq '{userId}'",
                maxPerPage: _maxHistoryMessages * 2)) // user + assistant pairs
            {
                messages.Add(msg);
            }

            // 對話逾時檢查：如果最後一則訊息超過 24 小時，清除所有歷史
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

            // Sort by RowKey (timestamp) ascending to maintain conversation order
            messages = messages.OrderBy(m => m.RowKey).ToList();

            // Add to chat history
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

    /// <summary>
    /// Get hourly request count for monitoring
    /// </summary>
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

            // 清除超過 1 小時的記錄
            _rateLimitTracker[userId].RemoveAll(t => t < oneHourAgo);

            // 記錄當前請求時間（用於統計）
            _rateLimitTracker[userId].Add(now);
            
            var count = _rateLimitTracker[userId].Count;
            _logger.LogInformation("User {UserId} request count: {Count} in the last hour", userId, count);
            
            return count;
        }
    }

    /// <summary>
    /// Save a message to conversation history
    /// </summary>
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

            // Cleanup old messages asynchronously (fire and forget)
            _ = CleanupOldMessagesAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {Role} message for user {UserId} after retries", role, userId);
        }
    }

    /// <summary>
    /// 將訊息暫存到 PendingMessages table，等待合併
    /// </summary>
    public async Task BufferPendingMessageAsync(string userId, string content, string replyToken)
    {
        try
        {
            await _retryPipeline.ExecuteAsync(async ct =>
            {
                var entity = new PendingMessageEntity
                {
                    PartitionKey = userId,
                    RowKey = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
                    Content = content,
                    ReplyToken = replyToken
                };

                await _pendingTableClient.AddEntityAsync(entity, ct);
                _logger.LogInformation("Buffered pending message for user {UserId}", userId);
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to buffer pending message for user {UserId}", userId);
            throw; // 往上拋，讓 caller 決定是否直接處理
        }
    }

    /// <summary>
    /// 等待 grouping window 後，取得並合併該用戶所有 pending 訊息。
    /// 回傳 null 代表有更新的訊息進來，本次 request 不需處理。
    /// </summary>
    public async Task<(string? combinedMessage, string? latestReplyToken)> WaitAndCollectMessagesAsync(string userId, string myRowKey)
    {
        // 等待 grouping window，讓後續連續訊息進入 buffer
        await Task.Delay(_messageGroupingWindow);

        // 查詢該用戶所有 pending 訊息
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

        // 按時間排序
        pendingMessages = pendingMessages.OrderBy(m => m.RowKey).ToList();
        var latestRowKey = pendingMessages.Last().RowKey;

        // 只有「最晚的那個 request」負責合併處理
        if (myRowKey != latestRowKey)
        {
            _logger.LogInformation("User {UserId}: newer message exists (my: {MyRowKey}, latest: {LatestRowKey}), skipping",
                userId, myRowKey, latestRowKey);
            return (null, null);
        }

        // 我是最新的 → 合併所有訊息
        var combinedMessage = string.Join("\n", pendingMessages.Select(m => m.Content));
        var latestReplyToken = pendingMessages.Last().ReplyToken;

        _logger.LogInformation("User {UserId}: combining {Count} messages into one",
            userId, pendingMessages.Count);

        // 清除所有 pending 訊息
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

    /// <summary>
    /// Remove old messages beyond the max limit
    /// </summary>
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
}
