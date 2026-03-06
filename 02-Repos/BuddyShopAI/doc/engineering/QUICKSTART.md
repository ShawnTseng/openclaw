# 🚀 Buddy ShopAI 快速開始

> 5 分鐘快速了解專案並運行第一個 AI 客服

---

## 專案簡介

**Buddy ShopAI** 是服飾電商專用的 LINE AI 智慧客服平台，支援多租戶獨立部署。

### 核心特點

- ⚡ **Serverless 架構** - Azure Functions，按用量付費
- 🤖 **AI 驅動** - Azure OpenAI gpt-4o-mini
- 👥 **多租戶** - 每個品牌獨立資源，完全隔離
- 💰 **超低成本** - 每租戶約 $78 TWD/月
- 🔐 **安全可靠** - Key Vault + Managed Identity

---

## 系統架構

```
LINE User
   │
   ▼
Azure Functions (.NET 8)
   ├─► Azure OpenAI (gpt-4o-mini)
   ├─► Table Storage (對話歷史)
   ├─► Key Vault (密鑰管理)
   └─► App Insights (監控)
```

---

## 前置需求

- .NET 8.0 SDK
- Azure Functions Core Tools v4
- Azure CLI (>= 2.50)
- LINE Messaging API 帳號
- Azure 訂閱

---

## 本地測試（可選）

快速在本地運行：

```bash
# 1. Clone 專案
git clone https://github.com/ShawnTseng/BuddyShopAI.git
cd BuddyShopAI

# 2. 設定環境變數
cp local.settings.json.example local.settings.json
# 編輯 local.settings.json 填入你的密鑰

# 3. 啟動本地 Storage 模擬器
npm install -g azurite
azurite --silent --location ~/.azurite

# 4. 啟動 Functions
dotnet restore && dotnet build
func start

# 5. 使用 ngrok 暴露到公網（測試用）
ngrok http 7071
# Webhook URL: https://YOUR_NGROK_URL/api/LineWebhook
```

---

## 雲端部署（生產環境）

詳細步驟請參考：[部署指南](DEPLOYMENT.md)

### 快速部署流程

```bash
# 1. 建立租戶設定
cp configs/_template.json configs/mytenant.json
# 編輯填入商店資訊

# 2. 建立部署參數
cp infra/main.parameters.template.json infra/main.parameters.mytenant.json
# 編輯填入 Azure 參數

# 3. 一鍵部署
./scripts/deploy-infra.sh mytenant
./scripts/deploy-app.sh mytenant

# 4. 設定 LINE Webhook
# 在 LINE Developers Console 填入:
# https://mytenant-func.azurewebsites.net/api/linewebhook
```

---

## 環境變數說明

| 變數 | 說明 |
|------|------|
| `TENANT_ID` | 租戶 ID（如 `mrvshop`） |
| `LINE__ChannelAccessToken` | LINE Channel Access Token |
| `LINE__ChannelSecret` | LINE Channel Secret |
| `AzureOpenAI__Endpoint` | Azure OpenAI 端點 |
| `AzureOpenAI__ApiKey` | Azure OpenAI API Key |
| `AzureOpenAI__DeploymentName` | 模型部署名稱（預設 `gpt-4o-mini`） |

---

## 驗證部署

```bash
# 測試 Webhook（應返回 401 或 400，表示簽章驗證正常）
curl -X POST https://mytenant-func.azurewebsites.net/api/linewebhook \
  -H "Content-Type: application/json" \
  -d '{"events":[]}'

# 查看即時日誌
func azure functionapp logstream mytenant-func
```

---

## 下一步

- 📖 [本地開發環境設定](LOCAL_DEVELOPMENT.md)
- ☁️ [完整部署指南](DEPLOYMENT.md)
- ⚙️ [配置管理](CONFIGURATION.md)
- 👥 [客戶上線流程](../business/ONBOARDING.md)

---

**問題?** 查看 [故障排除](TROUBLESHOOTING.md) 或聯繫維護者
