using Line.Messaging;
using BuddyShopAI.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.ApplicationInsights;
using Azure.Data.Tables;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register LINE Services
var channelAccessToken = builder.Configuration["LINE:ChannelAccessToken"] 
    ?? throw new InvalidOperationException("LINE Channel Access Token is not configured");

var channelSecret = builder.Configuration["LINE:ChannelSecret"] 
    ?? throw new InvalidOperationException("LINE Channel Secret is not configured");

builder.Services.AddSingleton<ILineMessagingClient>(_ => new LineMessagingClient(channelAccessToken));
builder.Services.AddSingleton(_ => new LineSignatureValidator(channelSecret));

// Register HttpClient for KeepWarmTimer
builder.Services.AddHttpClient();

// Register Azure Storage for Conversation History
var storageConnectionString = builder.Configuration["AzureWebJobsStorage"]
    ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured");

// Register TableServiceClient
builder.Services.AddSingleton<TableServiceClient>(serviceProvider =>
{
    return new TableServiceClient(storageConnectionString);
});

builder.Services.AddSingleton<ConversationHistoryService>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<ConversationHistoryService>>();
    return new ConversationHistoryService(storageConnectionString, logger);
});

// Register Prompt Provider
builder.Services.AddSingleton<PromptProvider>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<PromptProvider>>();
    return new PromptProvider(logger);
});

// Register Semantic Kernel with Azure OpenAI
builder.Services.AddSingleton<Kernel>(serviceProvider =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    
    var azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"]
        ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");
    var azureOpenAIApiKey = builder.Configuration["AzureOpenAI:ApiKey"]
        ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured");
    var deploymentName = builder.Configuration["AzureOpenAI:DeploymentName"]
        ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured");

    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: deploymentName,
        endpoint: azureOpenAIEndpoint,
        apiKey: azureOpenAIApiKey);
    
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Using Azure OpenAI (Deployment: {DeploymentName})", deploymentName);
    
    return kernelBuilder.Build();
});

// Register Application Insights TelemetryClient
builder.Services.AddSingleton<TelemetryClient>();

builder.Build().Run();
