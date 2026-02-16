# Buddy ShopAI - Azure 部署指南

> 最後更新：2026-02-13  
> 架構：Consumption Plan (最低成本)

---

## 📋 目錄

1. [系統架構](#系統架構)
2. [先決條件](#先決條件)
3. [首次部署（從零開始）](#首次部署從零開始)
4. [日常部署（程式碼更新）](#日常部署程式碼更新)
5. [環境變數設定](#環境變數設定)
6. [監控與日誌](#監控與日誌)
7. [故障排除](#故障排除)
8. [Azure OpenAI 管理](#azure-openai-管理)
9. [常用命令速查](#常用命令速查)

---

## 🏗️ 系統架構

\`\`\`
┌─────────────┐
│  LINE User  │
└──────┬──────┘
       │ Webhook (HTTPS)
       ▼
┌──────────────────────────────┐
│  Azure Functions             │
│  (.NET 8 Isolated Worker)    │
│  Consumption Plan (East US)  │
└──────┬───────────────────────┘
       │
       ├─► Azure OpenAI (gpt-4o-mini, East US)
       ├─► Azure Table Storage (對話歷史)
       ├─► Key Vault + Managed Identity (密鑰管理)
       └─► Application Insights (免費 5GB 遙測)
\`\`\`

### 資源命名規則

每個租戶的資源名稱遵循以下模式（以 `{TENANT_ID}` 代表租戶 ID）：

| 資源名稱模式 | 類型 | 區域 |
|---------|------|------|
| `rg-{TENANT_ID}-prod` | Resource Group | East US |
| `{TENANT_ID}-func` | Function App (Consumption) | East US |
| `EastUSLinuxDynamicPlan` | Dynamic Plan (共享) | East US |
| `{TENANT_ID}{random}` | Storage Account | East US |
| `kv{TENANT_ID}prod{random}` | Key Vault | East US |
| `{TENANT_ID}-openai-prod` | Azure OpenAI (gpt-4o-mini) | East US |
| `{TENANT_ID}-func` | Application Insights | East US |

### 成本估算

| 服務 | 月費用 | 說明 |
|------|--------|------|
| Azure OpenAI (gpt-4o-mini) | ~$2-3 | 200客/天×5問×30天 |
| Azure Functions | $0 | 免費額度內 (1M 執行/月) |
| Azure Storage | ~$0.01 | Table Storage ~10MB |
| Key Vault | ~$0.03 | Secret 操作 |
| Application Insights | $0 | 免費 5GB/月 |
| **總計** | **~$2.50 USD (~78 TWD)** | **遠低於 500 TWD 預算** |

---

## ✅ 先決條件

### 必要工具

\`\`\`bash
# 確認版本
az --version              # Azure CLI >= 2.50
func --version            # Azure Functions Core Tools >= 4.0
dotnet --version          # .NET SDK >= 8.0
\`\`\`

### 安裝指南

\`\`\`bash
# macOS
brew install azure-cli
brew tap azure/functions && brew install azure-functions-core-tools@4
brew install dotnet-sdk
\`\`\`

### Azure 帳號

- 訂閱 ID：透過 `az account show` 查詢
- 登入：`az login && az account set --subscription <YOUR_SUBSCRIPTION_ID>`

### LINE 平台

- LINE Developers 帳號
- Messaging API Channel
- Channel Access Token & Channel Secret

---

## 🚀 首次部署（從零開始）

> ⚠️ **重要**：此訂閱 VM 配額為 0，必須使用 Consumption Plan（共享 Dynamic Plan 繞過配額限制）。

> 💡 以下命令中 `${TENANT_ID}` 請替換為你的租戶 ID（如 `mrvshop`、`guban`）。  
> 建議先設定環境變數：`export TENANT_ID=mrvshop`

### Step 1: 登入 Azure

\`\`\`bash
az login
az account set --subscription <YOUR_SUBSCRIPTION_ID>
\`\`\`

### Step 2: 建立資源群組

\`\`\`bash
az group create --name rg-${TENANT_ID}-prod --location eastus
\`\`\`

### Step 3: 部署基礎設施 (Bicep)

\`\`\`bash
# 複製並填寫參數檔
cp infra/main.parameters.template.json infra/main.parameters.${TENANT_ID}.json
# 編輯 main.parameters.${TENANT_ID}.json，填入 LINE credentials

# 部署
az deployment group create \
  --resource-group rg-${TENANT_ID}-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.${TENANT_ID}.json
\`\`\`

> Bicep 會建立：Storage Account、Key Vault、Azure OpenAI、Function App

### Step 4: 設定 Managed Identity RBAC

\`\`\`bash
# 取得 Key Vault 名稱（由 Bicep 產生，含隨機後綴）
KV_NAME=$(az keyvault list --resource-group rg-${TENANT_ID}-prod --query "[0].name" -o tsv)

PRINCIPAL_ID=$(az functionapp show \
  --name ${TENANT_ID}-func \
  --resource-group rg-${TENANT_ID}-prod \
  --query "identity.principalId" -o tsv)

az role assignment create \
  --assignee "$PRINCIPAL_ID" \
  --role "Key Vault Secrets User" \
  --scope $(az keyvault show --name $KV_NAME --query id -o tsv)
\`\`\`

### Step 5: 設定 App Settings

\`\`\`bash
az functionapp config appsettings set \
  --name ${TENANT_ID}-func \
  --resource-group rg-${TENANT_ID}-prod \
  --settings \
    "LINE__ChannelAccessToken=@Microsoft.KeyVault(SecretUri=https://${KV_NAME}.vault.azure.net/secrets/LINE-ChannelAccessToken/)" \
    "LINE__ChannelSecret=@Microsoft.KeyVault(SecretUri=https://${KV_NAME}.vault.azure.net/secrets/LINE-ChannelSecret/)" \
    "AzureOpenAI__Endpoint=https://${TENANT_ID}-openai-prod.openai.azure.com/" \
    "AzureOpenAI__ApiKey=@Microsoft.KeyVault(SecretUri=https://${KV_NAME}.vault.azure.net/secrets/AzureOpenAI-ApiKey/)" \
    "AzureOpenAI__DeploymentName=gpt-4o-mini"
\`\`\`

### Step 6: 部署應用程式碼

\`\`\`bash
func azure functionapp publish ${TENANT_ID}-func
\`\`\`

### Step 7: 設定 LINE Webhook

1. 前往 [LINE Developers Console](https://developers.line.biz/console/)
2. 選擇你的 Messaging API Channel
3. 設定 Webhook URL：
   \`\`\`
   https://${TENANT_ID}-func.azurewebsites.net/api/linewebhook
   \`\`\`
4. 啟用 Webhook
5. 關閉「自動回覆訊息」

### Step 8: 驗證部署

\`\`\`bash
# 測試 webhook（預期回傳 401 = 簽章驗證正常運作）
curl -X POST https://${TENANT_ID}-func.azurewebsites.net/api/linewebhook \
  -H "Content-Type: application/json" \
  -d '{"events":[]}'

# 查看即時 log
func azure functionapp logstream ${TENANT_ID}-func
\`\`\`

---

## 📦 日常部署（程式碼更新）

程式碼修改後，只需一行命令重新部署：

\`\`\`bash
func azure functionapp publish ${TENANT_ID}-func
\`\`\`

或使用腳本：

\`\`\`bash
./scripts/deploy-app.sh ${TENANT_ID}
\`\`\`

---

## 🔐 環境變數設定

### 生產環境（透過 Key Vault References）

| 變數名稱 | 來源 | 說明 |
|----------|------|------|
| `LINE__ChannelAccessToken` | Key Vault | LINE Channel Access Token |
| `LINE__ChannelSecret` | Key Vault | LINE Channel Secret |
| `AzureOpenAI__Endpoint` | 直接值 | `https://{TENANT_ID}-openai-prod.openai.azure.com/` |
| `AzureOpenAI__ApiKey` | Key Vault | Azure OpenAI API Key |
| `AzureOpenAI__DeploymentName` | 直接值 | `gpt-4o-mini` |
| `AzureWebJobsStorage` | 自動 | Storage 連線字串（Bicep 設定） |

### 本地開發

\`\`\`bash
cp local.settings.json.example local.settings.json
# 編輯 local.settings.json，填入真實密鑰
\`\`\`

### 更新單一環境變數

\`\`\`bash
az functionapp config appsettings set \
  --name ${TENANT_ID}-func \
  --resource-group rg-${TENANT_ID}-prod \
  --settings "KEY=VALUE"
\`\`\`

---

## 📊 監控與日誌

### 即時 Log 串流

\`\`\`bash
func azure functionapp logstream ${TENANT_ID}-func
\`\`\`

### Application Insights 查詢

前往 Azure Portal → Application Insights (`${TENANT_ID}-func`) → Logs：

\`\`\`kusto
// 最近 24 小時的錯誤
traces
| where timestamp > ago(24h)
| where severityLevel >= 3
| order by timestamp desc

// Function 執行時間統計
requests
| where timestamp > ago(1h)
| summarize avg(duration), max(duration), count() by name

// AI API 呼叫次數
dependencies
| where timestamp > ago(24h)
| where type == "Http"
| summarize count() by target
\`\`\`

### 成本監控

\`\`\`bash
# 查看當月費用
az consumption usage list \
  --resource-group rg-${TENANT_ID}-prod \
  --start-date $(date -v1d +%Y-%m-%d) \
  --end-date $(date +%Y-%m-%d) \
  -o table
\`\`\`

---

## 🛠️ 故障排除

### 1. 訂閱 VM 配額為零

**症狀**：`SubscriptionIsOverQuotaForSku: This region has quota of 0 instances`

**原因**：此訂閱在所有區域的 VM 配額都是 0。

**解決**：使用 Consumption Plan（已採用），它使用共享的 Dynamic Plan，不需要 VM 配額。

\`\`\`bash
# 診斷：確認配額
az vm list-usage --location eastus --query "[?limit > '0']"
\`\`\`

### 2. Key Vault 存取被拒 (403)

**症狀**：Function App 啟動失敗，Key Vault Reference 無法解析。

**解決**：
\`\`\`bash
# 確認 Managed Identity 存在
az functionapp identity show --name ${TENANT_ID}-func --resource-group rg-${TENANT_ID}-prod

# 重新授予 RBAC
PRINCIPAL_ID=$(az functionapp show --name ${TENANT_ID}-func --resource-group rg-${TENANT_ID}-prod --query "identity.principalId" -o tsv)
KV_NAME=$(az keyvault list --resource-group rg-${TENANT_ID}-prod --query "[0].name" -o tsv)
az role assignment create \
  --assignee "$PRINCIPAL_ID" \
  --role "Key Vault Secrets User" \
  --scope $(az keyvault show --name $KV_NAME --query id -o tsv)
\`\`\`

### 3. LINE Webhook 回傳 401

**預期行為**：無效簽章的請求回傳 401 是正確的（代表驗證機制正常運作）。

**排查真正的問題**：
\`\`\`bash
# 確認 LINE Secret 是否正確設定
az keyvault secret show --vault-name ${KV_NAME} --name LINE-ChannelSecret --query "value" -o tsv
\`\`\`

### 4. AI API 429 錯誤 (Rate Limit)

**解決**：程式已內建 Exponential Backoff 重試機制（最多 3 次：1s → 2s → 4s）。

若持續發生，可調整 TPM：
\`\`\`bash
az cognitiveservices account deployment create \
  --resource-group rg-${TENANT_ID}-prod \
  --name ${TENANT_ID}-openai-prod \
  --deployment-name gpt-4o-mini \
  --model-name gpt-4o-mini \
  --model-version "2024-07-18" \
  --model-format OpenAI \
  --sku-capacity 60 \
  --sku-name "Standard"
\`\`\`

### 5. Function App 無法啟動

\`\`\`bash
# 檢查日誌
az functionapp log tail --name ${TENANT_ID}-func --resource-group rg-${TENANT_ID}-prod

# 重啟
az functionapp restart --name ${TENANT_ID}-func --resource-group rg-${TENANT_ID}-prod
\`\`\`

---

## 🤖 Azure OpenAI 管理

### 查看已部署模型

\`\`\`bash
az cognitiveservices account deployment list \
  --resource-group rg-${TENANT_ID}-prod \
  --name ${TENANT_ID}-openai-prod \
  -o table
\`\`\`

### 模型選擇參考

| 模型 | 成本 (每 1M tokens) | 適用場景 |
|------|---------------------|---------|
| **gpt-4o-mini** | Input $0.15 / Output $0.60 | **目前使用** - 最佳性價比 |
| gpt-4o | Input $5.00 / Output $15.00 | 需要最佳品質 |

### 取得 API Key

\`\`\`bash
az cognitiveservices account keys list \
  --resource-group rg-${TENANT_ID}-prod \
  --name ${TENANT_ID}-openai-prod \
  --query "key1" -o tsv
\`\`\`

---

## 📎 常用命令速查

\`\`\`bash
# === 部署 ===
func azure functionapp publish ${TENANT_ID}-func          # 部署程式碼
az functionapp restart --name ${TENANT_ID}-func -g rg-${TENANT_ID}-prod  # 重啟

# === 監控 ===
func azure functionapp logstream ${TENANT_ID}-func         # 即時 log
az resource list -g rg-${TENANT_ID}-prod -o table            # 列出所有資源

# === 設定 ===
az functionapp config appsettings list --name ${TENANT_ID}-func -g rg-${TENANT_ID}-prod -o table  # 查看設定
az functionapp config appsettings set --name ${TENANT_ID}-func -g rg-${TENANT_ID}-prod --settings "KEY=VALUE"  # 更新設定

# === Bicep ===
az bicep build --file infra/main.bicep                  # 編譯驗證
az deployment group validate -g rg-${TENANT_ID}-prod --template-file infra/main.bicep --parameters infra/main.parameters.${TENANT_ID}.json
az deployment group what-if -g rg-${TENANT_ID}-prod --template-file infra/main.bicep --parameters infra/main.parameters.${TENANT_ID}.json

# === 成本 ===
az consumption usage list -g rg-${TENANT_ID}-prod -o table
\`\`\`

---

## 📚 參考資源

- [Azure Functions 文件](https://learn.microsoft.com/azure/azure-functions/)
- [Bicep 語法參考](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [LINE Messaging API](https://developers.line.biz/en/docs/messaging-api/)
- [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/)
- [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/)

---

**文件版本**: 2.1.0  
**最後更新**: 2026-02-13