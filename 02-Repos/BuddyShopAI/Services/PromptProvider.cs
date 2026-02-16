using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuddyShopAI.Services;

/// <summary>
/// 提供商店知識庫與 System Prompt（支援多租戶）
/// </summary>
public class PromptProvider
{
    private readonly ILogger<PromptProvider> _logger;
    private readonly string _tenantId;
    private readonly string _configBasePath;
    private StoreConfig? _cachedConfig;

    public PromptProvider(ILogger<PromptProvider> logger, string? tenantId = null, string? configBasePath = null)
    {
        _logger = logger;
        _tenantId = tenantId ?? Environment.GetEnvironmentVariable("TENANT_ID") ?? "mrvshop";
        _configBasePath = configBasePath ?? Path.Combine(AppContext.BaseDirectory, "configs");
    }

    /// <summary>
    /// 取得商店知識庫資訊（包含店名、FAQ、回答原則等）
    /// </summary>
    public async Task<string> GetStoreKnowledgeBaseAsync()
    {
        try
        {
            var config = await LoadStoreConfigAsync();
            return BuildKnowledgeBase(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load store config, using fallback");
            return GetFallbackKnowledgeBase();
        }
    }

    /// <summary>
    /// 取得完整的 System Prompt（含知識庫）
    /// </summary>
    public async Task<string> GetSystemPromptAsync()
    {
        var knowledgeBase = await GetStoreKnowledgeBaseAsync();
        return $"你是一個服飾電商的智慧客服小幫手。請依據以下商店資訊回答顧客的問題：\n\n{knowledgeBase}";
    }

    private async Task<StoreConfig> LoadStoreConfigAsync()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        // 多租戶：優先載入 configs/{tenantId}.json
        var tenantConfigPath = Path.Combine(_configBasePath, $"{_tenantId}.json");
        
        // 向下相容：若 configs/ 不存在，嘗試載入根目錄的 store-config.json
        var legacyConfigPath = Path.Combine(AppContext.BaseDirectory, "store-config.json");
        
        string configPath;
        if (File.Exists(tenantConfigPath))
        {
            configPath = tenantConfigPath;
            _logger.LogInformation("Loading tenant config: {TenantId} from {Path}", _tenantId, configPath);
        }
        else if (File.Exists(legacyConfigPath))
        {
            configPath = legacyConfigPath;
            _logger.LogWarning("Tenant config not found for {TenantId}, falling back to legacy store-config.json", _tenantId);
        }
        else
        {
            _logger.LogWarning("No config file found for tenant {TenantId}", _tenantId);
            throw new FileNotFoundException($"Config not found for tenant: {_tenantId}");
        }

        var json = await File.ReadAllTextAsync(configPath);
        _cachedConfig = JsonSerializer.Deserialize<StoreConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize store config");

        _logger.LogInformation("Store config loaded successfully for tenant {TenantId} from {Path}", _tenantId, configPath);
        return _cachedConfig;
    }

    private string BuildKnowledgeBase(StoreConfig config)
    {
        var kb = new System.Text.StringBuilder();

        // 商店資訊
        kb.AppendLine("### 商店資訊");
        kb.AppendLine($"- 店名：{config.StoreName}");
        kb.AppendLine($"- 營業時間：{config.BusinessHours}");
        kb.AppendLine($"- 客服小編：{config.BotName} (AI 機器人)");
        if (!string.IsNullOrEmpty(config.ContactInfo))
        {
            kb.AppendLine($"- 聯絡資訊：{config.ContactInfo}");
        }
        kb.AppendLine();

        // 常見問答
        kb.AppendLine("### 常見問答 (FAQ)");
        for (int i = 0; i < config.FAQ.Count; i++)
        {
            var faq = config.FAQ[i];
            kb.AppendLine($"{i + 1}. **{faq.Question}**：");
            foreach (var answer in faq.Answers)
            {
                kb.AppendLine($"   - {answer}");
            }
        }
        kb.AppendLine();

        // 回答原則
        kb.AppendLine("### 你的回答原則");
        foreach (var guideline in config.ResponseGuidelines)
        {
            kb.AppendLine($"- {guideline}");
        }

        return kb.ToString();
    }

    private string GetFallbackKnowledgeBase()
    {
        return """
        ### 商店資訊
        - 店名：Buddy ShopAI (範例)
        - 營業時間：週一至週日 10:00 - 21:00
        - 客服小編：AI 小幫手 (AI 機器人)

        ### 常見問答 (FAQ)
        1. **運費計算**：
           - 全館滿 $1,500 免運。
           - 未滿免運門檻，宅配 $100、超商取貨 $60。
        2. **出貨時間**：
           - 現貨商品：下單後 1-2 個工作天內出貨。
           - 預購商品：需等待 7-14 個工作天。
        3. **退換貨政策**：
           - 收到商品後享有 7 天鑑賞期 (非試用期)。
           - 若商品有瑕疵，請在 3 天內拍照回傳，我們將全額負擔運費退換。
        4. **查訂單**：
           - 目前 AI 暫時無法直接查詢訂單系統。
           - **SOP**：請禮貌引導客人留下「訂單編號」與「手機號碼」，並告知「稍後會有真人小編為您查詢」。

        ### 你的回答原則
        - 語氣：親切、活潑、使用繁體中文 (台灣用語)。
        - 限制：絕對不要捏造事實。如果客人問的問題不在上述資料中，請回答：「這個問題稍微超出我的能力範圍，我已經幫您呼叫真人小編，請稍候喔！🙇‍♀️」
        - 格式：請勿使用 Markdown 語法 (如 **bold**)，因為 LINE 顯示會亂掉，請用純文字或 Emoji 排版。
        """;
    }
}

#region Data Models

public class StoreConfig
{
    public string StoreName { get; set; } = string.Empty;
    public string BusinessHours { get; set; } = string.Empty;
    public string BotName { get; set; } = "小S";
    public string ContactInfo { get; set; } = string.Empty;
    public List<FaqItem> FAQ { get; set; } = new();
    public List<string> ResponseGuidelines { get; set; } = new();
}

public class FaqItem
{
    public string Question { get; set; } = string.Empty;
    public List<string> Answers { get; set; } = new();
}

#endregion
