using Line.Messaging;
using BuddyShopAI.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;

namespace BuddyShopAI;

public class HumanModeTimeoutTimer
{
    private readonly ILogger<HumanModeTimeoutTimer> _logger;
    private readonly UserModeService _userModeService;
    private readonly ILineMessagingClient _lineClient;
    private readonly TelemetryClient _telemetryClient;

    private static readonly TimeSpan HumanModeTimeout = TimeSpan.FromHours(24);

    public HumanModeTimeoutTimer(
        ILogger<HumanModeTimeoutTimer> logger,
        UserModeService userModeService,
        ILineMessagingClient lineClient,
        TelemetryClient telemetryClient)
    {
        _logger = logger;
        _userModeService = userModeService;
        _lineClient = lineClient;
        _telemetryClient = telemetryClient;
    }

    [Function("HumanModeTimeoutTimer")]
    public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("⏰ HumanModeTimeoutTimer triggered at: {Time}", DateTime.UtcNow);

        try
        {
            var resetUserIds = await _userModeService.ResetExpiredHumanModesAsync(HumanModeTimeout);

            if (resetUserIds.Count == 0)
            {
                _logger.LogInformation("✅ No expired human-mode users found");
                return;
            }

            _logger.LogInformation(
                "⏰ Auto-reset {Count} users from human mode: {UserIds}",
                resetUserIds.Count,
                string.Join(", ", resetUserIds.Select(id => id[..Math.Min(12, id.Length)] + "...")));

            _telemetryClient.TrackEvent("HumanModeAutoReset", new Dictionary<string, string>
            {
                ["resetCount"] = resetUserIds.Count.ToString(),
                ["userIds"] = string.Join(",", resetUserIds)
            });

            var manageUserId = Environment.GetEnvironmentVariable("Manage__LineUserId");
            if (!string.IsNullOrEmpty(manageUserId))
            {
                var userList = string.Join("\n",
                    resetUserIds.Select(id => $"• {id[..Math.Min(12, id.Length)]}..."));

                var notification =
                    $"⏰ 超時自動重置通知\n\n" +
                    $"以下 {resetUserIds.Count} 位用戶已超過 24 小時未處理，已自動切回 AI 模式：\n\n" +
                    $"{userList}\n\n" +
                    $"如需重新切換為真人模式，請使用 /manage human {{userId}}";

                try
                {
                    await _lineClient.PushMessageAsync(manageUserId, new[] { new TextMessage(notification) });
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, "Failed to notify manager about auto-reset");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during human mode timeout check");
            _telemetryClient.TrackException(ex);
        }

        if (timerInfo.ScheduleStatus != null)
        {
            _logger.LogInformation("⏰ Next timeout check at: {NextRun}", timerInfo.ScheduleStatus.Next);
        }
    }
}

