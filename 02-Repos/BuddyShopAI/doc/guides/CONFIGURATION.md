# ⚙️ 配置管理指南

> 租戶配置與參數管理

---

## 租戶配置檔案

### 配置檔位置

```
configs/
├── _template.json        # 新客戶範本
├── mrvshop.json          # 88MRV 潮流選貨店
└── guban.json            # 古班服飾
```

### 配置結構

```json
{
  "storeName": "88MRV 潮流選貨店",
  "businessHours": "每日 11:00 - 22:00",
  "shippingInfo": "全家/711 取貨付款 $60，滿 $2000 免運",
  "returnPolicy": "七天鑑賞期，商品未使用可退換",
  "socialMedia": {
    "instagram": "@88mrv.tw",
    "facebook": "https://www.facebook.com/88mrv"
  },
  "aiPersonality": "親切可愛，使用 emoji 🎉，年輕活潑",
  "faq": [
    {
      "category": "運費",
      "questions": [
        "怎麼寄？",
        "運費多少？"
      ],
      "answer": "我們提供全家/7-11 超商取貨付款，運費 $60！滿 $2000 免運費唷 🎁"
    }
  ]
}
```

### 自動載入機制

`PromptProvider` 根據環境變數 `TENANT_ID` 自動載入：

```csharp
var tenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? "mrvshop";
var configPath = $"configs/{tenantId}.json";
```

---

## 環境變數

### 必要變數

| 變數 | 說明 | 範例 |
|------|------|------|
| `TENANT_ID` | 租戶識別碼 | `mrvshop` |
| `AzureWebJobsStorage` | Storage 連接字串 | 從 Bicep 自動設定 |
| `LINE__ChannelAccessToken` | LINE Token | Key Vault Reference |
| `LINE__ChannelSecret` | LINE Secret | Key Vault Reference |
| `AzureOpenAI__Endpoint` | OpenAI 端點 | `https://{tenant}-openai-prod.openai.azure.com/` |
| `AzureOpenAI__ApiKey` | OpenAI Key | Key Vault Reference |
| `AzureOpenAI__DeploymentName` | 模型名稱 | `gpt-4o-mini` |

### 本地開發

`local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "TENANT_ID": "demo",
    "LINE__ChannelAccessToken": "YOUR_TOKEN",
    "LINE__ChannelSecret": "YOUR_SECRET",
    "AzureOpenAI__Endpoint": "https://your-openai.openai.azure.com/",
    "AzureOpenAI__ApiKey": "YOUR_KEY",
    "AzureOpenAI__DeploymentName": "gpt-4o-mini"
  }
}
```

---

## 配置變更流程

### 方法 1: 修改 JSON 並重新部署（目前）

```bash
# 1. 編輯配置
vi configs/mrvshop.json

# 2. 重新部署應用程式
./scripts/deploy-app.sh mrvshop
```

### 方法 2: Google Sheets CMS（規劃中）

- 店家直接在 Google Sheet 編輯
- Function App 定期抓取 CSV
- 無需重新部署

---

## Bicep 參數檔

### 參數檔位置

```
infra/
├── main.parameters.template.json    # 範本
├── main.parameters.mrvshop.json     # mrvshop 專用
└── main.parameters.guban.json       # guban 專用
```

### 參數結構

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "appNamePrefix": {
      "value": "mrvshop"
    },
    "location": {
      "value": "eastus"
    },
    "lineChannelAccessToken": {
      "value": "YOUR_LINE_TOKEN"
    },
    "lineChannelSecret": {
      "value": "YOUR_LINE_SECRET"
    },
    "azureOpenAIApiKey": {
      "value": "YOUR_OPENAI_KEY"
    }
  }
}
```

---

## 新增租戶流程

### 1. 建立配置檔

```bash
cp configs/_template.json configs/newclient.json
# 編輯 configs/newclient.json
```

### 2. 建立參數檔

```bash
cp infra/main.parameters.template.json infra/main.parameters.newclient.json
# 編輯 infra/main.parameters.newclient.json
```

### 3. 部署

```bash
./scripts/deploy-infra.sh newclient
./scripts/deploy-app.sh newclient
```

---

## 配置最佳實踐

✅ **DO**
- 使用範本建立新配置
- 配置檔加入 Git（除敏感資訊外）
- FAQ 分類清晰
- 測試後再部署

❌ **DON'T**
- 將 API Key 寫入配置檔
- 直接修改 `_template.json`
- 忘記更新 FAQ
- 跳過測試直接上線

---

詳細說明: [客戶上線流程](../business/ONBOARDING.md)
