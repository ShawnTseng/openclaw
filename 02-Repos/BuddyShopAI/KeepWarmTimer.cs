using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BuddyShopAI;

/// <summary>
/// Timer Trigger Function: 每 5 分鐘自動呼叫 /api/health endpoint
/// 目的：防止 Azure Functions Consumption Plan 冷啟動
/// 成本：$0 (在免費額度內)
/// </summary>
public class KeepWarmTimer
{
    private readonly ILogger<KeepWarmTimer> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public KeepWarmTimer(ILogger<KeepWarmTimer> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 每 5 分鐘執行一次，呼叫 health check endpoint 保持 function app warm
    /// Cron 格式: {秒} {分} {時} {日} {月} {週}
    /// "0 */5 * * * *" = 每 5 分鐘的第 0 秒執行
    /// </summary>
    [Function("KeepWarmTimer")]
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("🔥 KeepWarm Timer triggered at: {Time}", DateTime.UtcNow);

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            // 呼叫本機 health check endpoint (在同一個 function app 內)
            // Azure Functions 會自動解析成正確的 URL
            var healthUrl = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") != null
                ? $"https://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/health"
                : "http://localhost:7071/api/health"; // Local development fallback

            _logger.LogInformation("🏓 Pinging health endpoint: {Url}", healthUrl);

            var response = await httpClient.GetAsync(healthUrl);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Health check passed! Status: {Status}, Response: {Response}", 
                    response.StatusCode, content);
            }
            else
            {
                _logger.LogWarning("⚠️ Health check returned non-success status: {Status}, Response: {Response}", 
                    response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during keep-warm health check");
        }

        // 記錄下次執行時間
        if (timerInfo.ScheduleStatus != null)
        {
            _logger.LogInformation("⏰ Next timer schedule at: {NextRun}", timerInfo.ScheduleStatus.Next);
        }
    }
}
