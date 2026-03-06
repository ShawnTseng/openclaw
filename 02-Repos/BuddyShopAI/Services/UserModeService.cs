using Azure;
using Azure.Data.Tables;
using BuddyShopAI.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace BuddyShopAI.Services;

public class UserModeService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<UserModeService> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    public UserModeService(string connectionString, ILogger<UserModeService> logger)
    {
        _logger = logger;
        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient("UserModes");
        _tableClient.CreateIfNotExists();

        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("Retrying UserMode Table operation. Attempt: {Attempt}", args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<bool> IsHumanModeAsync(string userId)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<UserModeEntity>("UserMode", userId);
            return response.Value.Mode == "human";
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false; // 預設：AI 模式
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user mode for {UserId}, defaulting to AI mode", userId);
            return false;
        }
    }

    public async Task SetUserModeAsync(string userId, string mode, string? note = null)
    {
        var entity = new UserModeEntity
        {
            PartitionKey = "UserMode",
            RowKey = userId,
            Mode = mode,
            ChangedAt = DateTime.UtcNow,
            Note = note
        };

        await _retryPipeline.ExecuteAsync(async ct =>
        {
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
            _logger.LogInformation("User {UserId} mode set to {Mode}", userId, mode);
        }, CancellationToken.None);
    }

    public async Task<List<UserModeEntity>> GetAllUserModesAsync()
    {
        var results = new List<UserModeEntity>();
        await foreach (var entity in _tableClient.QueryAsync<UserModeEntity>(
            filter: "PartitionKey eq 'UserMode'"))
        {
            results.Add(entity);
        }
        return results.OrderByDescending(e => e.ChangedAt).ToList();
    }

    public async Task<UserModeEntity> GetUserModeAsync(string userId)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<UserModeEntity>("UserMode", userId);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return new UserModeEntity
            {
                RowKey = userId,
                Mode = "ai",
                ChangedAt = DateTime.MinValue
            };
        }
    }

    public async Task<List<string>> ResetExpiredHumanModesAsync(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;
        var resetUserIds = new List<string>();

        var allModes = await GetAllUserModesAsync();
        var expiredUsers = allModes
            .Where(m => m.Mode == "human" && m.ChangedAt < cutoff)
            .ToList();

        foreach (var user in expiredUsers)
        {
            try
            {
                await SetUserModeAsync(
                    user.RowKey,
                    "ai",
                    $"24小時超時自動重置（原切換時間：{user.ChangedAt:MM/dd HH:mm} UTC）");
                resetUserIds.Add(user.RowKey);
                _logger.LogInformation(
                    "⏰ Auto-reset expired human mode for user {UserId}. Was set at {ChangedAt}",
                    user.RowKey, user.ChangedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-reset human mode for user {UserId}", user.RowKey);
            }
        }

        return resetUserIds;
    }
}

