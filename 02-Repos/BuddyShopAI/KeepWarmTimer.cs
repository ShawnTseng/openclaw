using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BuddyShopAI;

public class KeepWarmTimer
{
    private readonly ILogger<KeepWarmTimer> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public KeepWarmTimer(ILogger<KeepWarmTimer> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [Function("KeepWarmTimer")]
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("🔥 KeepWarm Timer triggered at: {Time}", DateTime.UtcNow);

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
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

        if (timerInfo.ScheduleStatus != null)
        {
            _logger.LogInformation("⏰ Next timer schedule at: {NextRun}", timerInfo.ScheduleStatus.Next);
        }
    }
}

