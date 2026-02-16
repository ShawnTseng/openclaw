using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuddyShopAI;

public class HealthCheck
{
    private readonly ILogger<HealthCheck> _logger;
    private readonly IConfiguration _configuration;
    private readonly TableServiceClient _tableServiceClient;

    public HealthCheck(
        ILogger<HealthCheck> logger,
        IConfiguration configuration,
        TableServiceClient tableServiceClient)
    {
        _logger = logger;
        _configuration = configuration;
        _tableServiceClient = tableServiceClient;
    }

    [Function("HealthCheck")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        var healthStatus = new HealthCheckResult
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Checks = new Dictionary<string, ComponentHealth>()
        };

        // Check Table Storage
        try
        {
            var tableClient = _tableServiceClient.GetTableClient("ConversationHistory");
            await tableClient.CreateIfNotExistsAsync();
            healthStatus.Checks["tableStorage"] = new ComponentHealth { Status = "healthy" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table Storage health check failed");
            healthStatus.Checks["tableStorage"] = new ComponentHealth 
            { 
                Status = "unhealthy", 
                Error = ex.Message 
            };
            healthStatus.Status = "degraded";
        }

        // Check Azure OpenAI Configuration
        try
        {
            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var deploymentName = _configuration["AzureOpenAI:DeploymentName"];
            
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
                throw new Exception("Missing Azure OpenAI configuration");

            healthStatus.Checks["azureOpenAI"] = new ComponentHealth 
            { 
                Status = "healthy",
                Details = new Dictionary<string, string>
                {
                    ["endpoint"] = endpoint,
                    ["deploymentName"] = deploymentName
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI configuration check failed");
            healthStatus.Checks["azureOpenAI"] = new ComponentHealth 
            { 
                Status = "unhealthy", 
                Error = ex.Message 
            };
            healthStatus.Status = "degraded";
        }

        // Check LINE Configuration
        try
        {
            var channelAccessToken = _configuration["LINE:ChannelAccessToken"];
            var channelSecret = _configuration["LINE:ChannelSecret"];

            if (string.IsNullOrEmpty(channelAccessToken) || string.IsNullOrEmpty(channelSecret))
                throw new Exception("Missing LINE configuration");

            healthStatus.Checks["lineMessaging"] = new ComponentHealth { Status = "healthy" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LINE configuration check failed");
            healthStatus.Checks["lineMessaging"] = new ComponentHealth 
            { 
                Status = "unhealthy", 
                Error = ex.Message 
            };
            healthStatus.Status = "degraded";
        }

        var statusCode = healthStatus.Status == "healthy" ? 200 : 503;
        
        return new JsonResult(healthStatus) { StatusCode = statusCode };
    }
}

public class HealthCheckResult
{
    public string Status { get; set; } = "unknown";
    public DateTime Timestamp { get; set; }
    public Dictionary<string, ComponentHealth> Checks { get; set; } = new();
}

public class ComponentHealth
{
    public string Status { get; set; } = "unknown";
    public string? Error { get; set; }
    public Dictionary<string, string>? Details { get; set; }
}
