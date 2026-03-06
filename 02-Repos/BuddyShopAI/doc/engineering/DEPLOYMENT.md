# Buddy ShopAI - Azure 部署指南

> 最後更新：2026-02-22
> 架構：Flex Consumption (Production, 無冷啟動) + Consumption Plan (Staging) 雙環境

---

## 📋 目錄

1. [環境架構](#環境架構)
2. [先決條件](#先決條件)
3. [首次佈建（Provision）](#首次佈建provision)
4. [日常部署流程](#日常部署流程)
5. [GitHub Actions 工作流程](#github-actions-工作流程)
6. [本地腳本部署](#本地腳本部署)
7. [環境變數設定](#環境變數設定)
8. [監控與日誌](#監控與日誌)
9. [故障排除](#故障排除)
10. [新增客戶 Checklist](#新增客戶-checklist)
11. [常用命令速查](#常用命令速查)

---

## 🏗️ 環境架構

### 雙環境設計

```
                          ┌─────────────────────────┐
                          │      LINE Platform       │
                          │    (Webhook Request)     │
                          └────────┬────────┬────────┘
                                   │        │
               ┌───────────────────┘        └───────────────────┐
               ▼                                                ▼
┌──────────────────────────────┐          ┌──────────────────────────────┐
│   rg-mrvshop-staging         │          │   rg-mrvshop-prod            │
│   (Staging · Consumption)    │          │   (Production · Flex)        │
├──────────────────────────────┤          ├──────────────────────────────┤
│ mrvshop-func-staging         │          │ mrvshop-func                 │
│ mrvshopstaging{hash} (Store) │          │ mrvshop{hash}     (Storage)  │
│ kvmrvshopstaging{h}  (KV)    │  ─swap→  │ kvmrvshopprod{h}   (KV)      │
│ mrvshop-openai-staging (AI)  │          │ mrvshop-openai-prod  (AI)    │
│ mrvshop-appinsights-staging  │          │ mrvshop-appinsights          │
└──────────────────────────────┘          └──────────────────────────────┘
```

### 命名規則

| 環境 | Resource Group | Function App | Storage | Key Vault | OpenAI | App Insights |
|------|---------------|-------------|---------|-----------|--------|-------------|
| **Staging** | `rg-{customer}-staging` | `{customer}-func-staging` | `{customer}staging{hash}` | `kv{customer}staging{hash}` | `{customer}-openai-staging` | `{customer}-appinsights-staging` |
| **Production** | `rg-{customer}-prod` | `{customer}-func` | `{customer}{hash}` | `kv{customer}prod{hash}` | `{customer}-openai-prod` | `{customer}-appinsights` |

> `{hash}` 由 `uniqueString(resourceGroup().id)` 自動產生。

### 部署流程

```
dev push → 自動部署 Staging → 手動觸發 Promote → 部署到 Production
                                      ↑
                             verify-staging → build → deploy-prod → verify-prod
```

### 設計原則

- ✅ **RG 分開** — 成本追蹤清楚、風險隔離、一鍵清除
- ✅ **Storage 分開** — 業務資料隔離（對話歷史、用戶模式不互相污染）
- ✅ **OpenAI 分開** — Rate limit 獨立、用量分開計費
- ✅ **Key Vault 分開** — Staging 可用測試密鑰
- ✅ **Production Storage 命名向後相容** — 不影響現有資料
- ✅ **Flex Consumption (Production)** — Always Ready 1 instance，無冷啟動
- ✅ **Consumption Plan (Staging)** — 零固定成本
- ✅ **預設 East Asia** — 離台灣最近的 Azure 區域
- ✅ **OpenAI 區域分離** — `gpt-4o-mini GlobalStandard` 在 `eastasia` 不可用，改用 `japaneast`（第二近）

### Storage Account 內容說明

```
Storage Account
├── 📁 Blob Containers
│   ├── azure-webjobs-hosts     ← lock / timer trigger 狀態
│   ├── azure-webjobs-secrets   ← function keys / host keys
│   ├── scm-releases            ← 部署 artifact
│   └── deploymentpackage       ← Flex Consumption 部署套件（Production only）
├── 📁 File Shares（Consumption Plan 必需，Staging only）
│   └── {function-app-name}     ← 程式碼根目錄 (WEBSITE_CONTENTSHARE)
├── 📁 Queues（Runtime 內部使用）
│   └── webjobs-*               ← internal poison queue
└── 📁 Tables（App 業務資料）
    ├── ConversationHistory      ← 對話歷史（最近 10 則 / 用戶）
    ├── PendingMessages          ← 訊息合併暫存（3 秒 grouping window）
    └── UserModes                ← 真人 / AI 模式切換狀態
```

### 成本估算（每客戶 · 兩環境合計）

| 服務 | Production | Staging | 說明 |
|------|-----------|---------|------|
| Azure OpenAI (gpt-4o-mini) | ~$2-3 | ~$0.50 | Staging 流量低 |
| Azure Functions | ~$5 | $0 | Prod: Flex Always Ready / Staging: 免費額度 |
| Azure Storage | ~$0.01 | ~$0.01 | Table Storage |
| Key Vault | ~$0.03 | ~$0.03 | Secret 操作 |
| Application Insights | $0 | $0 | 免費 5GB/月 |
| **合計** | **~$7.50** | **~$0.55** | **~$8 USD/月** |

---

## ✅ 先決條件

### 必要工具

```bash
az --version              # Azure CLI >= 2.50
func --version            # Azure Functions Core Tools >= 4.0
dotnet --version          # .NET SDK >= 8.0
```

### 安裝指南

```bash
# macOS
brew install azure-cli
brew tap azure/functions && brew install azure-functions-core-tools@4
brew install dotnet-sdk
```

### Azure 帳號

```bash
az login
az account set --subscription <YOUR_SUBSCRIPTION_ID>
```

### GitHub Secrets（GitHub Actions 使用）

| Secret 名稱 | 說明 |
|-------------|------|
| `AZURE_CREDENTIALS` | Azure 服務主體 JSON |
| `MRVSHOP_LINE_TOKEN` | mrvshop LINE Channel Access Token |
| `MRVSHOP_LINE_SECRET` | mrvshop LINE Channel Secret |
| `MRVSHOP_OPENAI_KEY` | mrvshop Azure OpenAI Key（選填，Bicep 會自動建立） |

---

## 🚀 首次佈建（Provision）

> ⚠️ 此訂閱 VM 配額為 0。Staging 使用 Consumption Plan (共享 Dynamic Plan) 不需 VM 配額；Production 使用 Flex Consumption Plan 也不需 VM 配額。

### 方式 A：GitHub Actions（推薦）

前往 GitHub → Actions → **Provision Infrastructure** → Run workflow：

1. **Provision Staging**：customer=`mrvshop`, environment=`staging`, location=`eastasia`
2. **Provision Production**：customer=`mrvshop`, environment=`production`, location=`eastasia`

> Bicep 自動處理所有命名（包括 env suffix）、建立 Storage、Key Vault、OpenAI、Function App、RBAC。

### 方式 B：本地腳本

```bash
# 1. 建立參數檔（已存在可跳過）
cp infra/main.parameters.template.json infra/main.parameters.mrvshop.json
# 編輯 main.parameters.mrvshop.json 填入 LINE credentials

# 2. Provision Staging
./scripts/deploy-infra.sh mrvshop staging

# 3. Provision Production
./scripts/deploy-infra.sh mrvshop production
```

### 方式 C：手動 Azure CLI

```bash
export TENANT_ID=mrvshop

# Staging
az group create --name rg-${TENANT_ID}-staging --location eastasia
az deployment group create \
  --resource-group rg-${TENANT_ID}-staging \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.${TENANT_ID}.json \
  --parameters environment=staging

# Production
az group create --name rg-${TENANT_ID}-prod --location eastasia
az deployment group create \
  --resource-group rg-${TENANT_ID}-prod \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.${TENANT_ID}.json \
  --parameters environment=production
```

### 佈建後驗證

```bash
az resource list -g rg-mrvshop-staging -o table
az resource list -g rg-mrvshop-prod -o table
```

### 設定 LINE Webhook

1. 前往 [LINE Developers Console](https://developers.line.biz/console/)
2. 選擇 Messaging API Channel
3. 設定 Webhook URL：
   - Production: `https://mrvshop-func.azurewebsites.net/api/linewebhook`
   - Staging（測試用）: `https://mrvshop-func-staging.azurewebsites.net/api/linewebhook`
4. 啟用 Webhook，關閉「自動回覆訊息」

---

## 📦 日常部署流程

> **部署方式**：統一使用 `az functionapp deployment source config-zip`。
> 此方法同時支援 Consumption Plan (Staging) 與 Flex Consumption (Production)。
> ⚠️ 不要使用 `func azure functionapp publish` 或 `az functionapp deploy --type zip`，
> 這些方式在 Flex Consumption 上不可靠。
> ⚠️ Staging 部署前會自動刪除 `WEBSITE_RUN_FROM_PACKAGE` 設定，避免衝突。

### 標準流程：dev → staging → production

```
1. Push code to dev branch
   └→ GitHub Actions 自動部署到 Staging (deploy.yml)

2. 驗證 Staging OK
   └→ 手動觸發 Promote workflow (promote.yml)
      └→ 驗證 staging 健康 → 重新建構 → 部署 Production → 驗證 Production
```

### 緊急修復：直接部署 Production

```
GitHub Actions → Deploy to Azure Functions → 手動選 environment=production
```

> ⚠️ 跳過 staging 驗證，僅在緊急情況使用。

---

## 🔄 GitHub Actions 工作流程

### 1. `deploy.yml` — 部署程式碼

| 觸發方式 | 目標環境 | 說明 |
|---------|---------|------|
| `push` to `dev` | Staging | 自動部署，CI/CD |
| 手動選 `staging` | Staging | 重新部署到 staging |
| 手動選 `production` | Production | 直接部署 production（緊急用） |

### 2. `provision.yml` — 佈建基礎設施

手動觸發，為指定客戶 + 環境建立 Azure 資源（Storage、Key Vault、OpenAI、Function App）。

### 3. `promote.yml` — Staging → Production（模擬 Slot Swap）

```
verify-staging → build (from dev HEAD) → deploy production → verify-production
```

> Consumption / Flex Consumption Plan 不支援 Deployment Slot，本 workflow 以「重新建構相同程式碼並部署」取代 slot swap。
> 可在 GitHub Environment 設定 Required reviewers 加審核關卡。

### 4. `keep-warm.yml` — 保持溫暖

每 4 分鐘 ping Staging Function App 的 `/api/health`，防止 Consumption Plan 冷啟動。
Production 使用 Flex Consumption + Always Ready，不需要 keep-warm。

---

## 🖥️ 本地腳本部署

### deploy-infra.sh — 佈建基礎設施

```bash
./scripts/deploy-infra.sh <tenant-id> [environment] [location] [params-file]

# 範例
./scripts/deploy-infra.sh mrvshop staging              # Staging (default: eastasia)
./scripts/deploy-infra.sh mrvshop production            # Production
./scripts/deploy-infra.sh mrvshop production eastasia   # 指定區域
```

### deploy-app.sh — 部署應用程式

```bash
./scripts/deploy-app.sh <tenant-id> [environment]

# 範例
./scripts/deploy-app.sh mrvshop staging       # 部署到 Staging
./scripts/deploy-app.sh mrvshop production    # 部署到 Production
```

### deploy-all.sh — 批量部署所有租戶

```bash
./scripts/deploy-all.sh [infra|app|both] [staging|production]

# 範例
./scripts/deploy-all.sh app staging           # 所有租戶部署 app 到 staging
./scripts/deploy-all.sh both production       # 所有租戶部署 infra + app 到 production
```

---

## 🔐 環境變數設定

### Azure 環境（透過 Bicep + Key Vault References 自動設定）

| 變數名稱 | 來源 | 說明 |
|----------|------|------|
| `TENANT_ID` | 直接值 | 租戶 ID（用於載入 configs/） |
| `LINE__ChannelAccessToken` | Key Vault Reference | LINE Channel Access Token |
| `LINE__ChannelSecret` | Key Vault Reference | LINE Channel Secret |
| `AzureOpenAI__Endpoint` | 直接值 | Azure OpenAI endpoint URL |
| `AzureOpenAI__ApiKey` | Key Vault Reference | Azure OpenAI API Key |
| `AzureOpenAI__DeploymentName` | 直接值 | `gpt-4o-mini` |
| `AzureWebJobsStorage` | 直接值 | Storage Account 連線字串 |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | 直接值 | Application Insights |
| `Manage__ApiKey` | 直接值 | 管理 API 認證金鑰 |
| `Manage__LineUserId` | 直接值 | 管理員 LINE UserId（LINE 指令授權） |

### 本地開發

```bash
cp local.settings.json.example local.settings.json
# 編輯填入真實密鑰
# AzureWebJobsStorage = "UseDevelopmentStorage=true" → 使用 Azurite 模擬器
```

本地開發使用 Azurite 模擬 Storage（含 Table Storage）：

```bash
# 安裝
npm install -g azurite     # 或安裝 VS Code Extension "Azurite"

# 啟動
azurite --silent --location /tmp/azurite

# 啟動 Function App
func start
```

### 更新單一環境變數

```bash
# Staging
az functionapp config appsettings set \
  --name mrvshop-func-staging \
  --resource-group rg-mrvshop-staging \
  --settings "KEY=VALUE"

# Production
az functionapp config appsettings set \
  --name mrvshop-func \
  --resource-group rg-mrvshop-prod \
  --settings "KEY=VALUE"
```

---

## 📊 監控與日誌

### 即時 Log 串流

```bash
func azure functionapp logstream mrvshop-func-staging    # Staging
func azure functionapp logstream mrvshop-func            # Production
```

### Application Insights 查詢

前往 Azure Portal → Application Insights → Logs：

```kusto
// 最近 24 小時的錯誤
traces
| where timestamp > ago(24h)
| where severityLevel >= 3
| order by timestamp desc

// Function 執行時間統計
requests
| where timestamp > ago(1h)
| summarize avg(duration), max(duration), count() by name
```

### 成本監控

```bash
az consumption usage list -g rg-mrvshop-staging -o table
az consumption usage list -g rg-mrvshop-prod -o table
```

---

## 🛠️ 故障排除

### 1. 訂閱 VM 配額為零

**症狀**：`SubscriptionIsOverQuotaForSku`
**解決**：Staging 使用 Consumption Plan、Production 使用 Flex Consumption，皆不需要 VM 配額。

### 2. Key Vault 存取被拒 (403)

**解決**：Bicep 已自動設定 RBAC。重新 provision 即可修復。

```bash
az functionapp identity show --name mrvshop-func --resource-group rg-mrvshop-prod
```

### 3. LINE Webhook 回傳 401

**預期行為**：無效簽章回傳 401 是正確的（代表驗證機制正常運作）。

### 4. Function App 無法啟動

```bash
az functionapp log tail --name mrvshop-func --resource-group rg-mrvshop-prod
az functionapp restart --name mrvshop-func --resource-group rg-mrvshop-prod
```

### 5. AI API 429 錯誤 (Rate Limit)

程式已內建 Exponential Backoff 重試機制。若持續發生，可調整 TPM：

```bash
az cognitiveservices account deployment create \
  --resource-group rg-mrvshop-prod \
  --name mrvshop-openai-prod \
  --deployment-name gpt-4o-mini \
  --model-name gpt-4o-mini \
  --model-version "2024-07-18" \
  --model-format OpenAI \
  --sku-capacity 60 \
  --sku-name "Standard"
```

---

## 🆕 新增客戶 Checklist

以 `guban` 為例：

### 1. 建立參數檔

```bash
cp infra/main.parameters.template.json infra/main.parameters.guban.json
# 編輯填入 guban 的 LINE credentials
```

### 2. 建立客戶設定檔

```bash
cp configs/_template.json configs/guban.json
# 編輯填入 guban 的 prompt、FAQ 等
```

### 3. 設定 GitHub Secrets

在 GitHub repo Settings → Secrets 新增：
- `GUBAN_LINE_TOKEN`
- `GUBAN_LINE_SECRET`
- `GUBAN_OPENAI_KEY`（選填）

### 4. Provision 基礎設施

GitHub Actions → **Provision Infrastructure**：
1. customer=`guban`, environment=`staging` → 佈建 staging
2. customer=`guban`, environment=`production` → 佈建 production

### 5. 部署應用程式碼

GitHub Actions → **Deploy to Azure Functions**：
1. customer=`guban`, environment=`staging` → 部署到 staging
2. 驗證 staging OK → **Promote** 到 production

### 6. 啟用 Keep Warm

編輯 `.github/workflows/keep-warm.yml`，取消註解 guban 的項目。

### 7. 設定 LINE Webhook

Webhook URL: `https://guban-func.azurewebsites.net/api/linewebhook`

### 8. 更新腳本

編輯 `scripts/deploy-all.sh`，加入 guban：
```bash
TENANTS=("mrvshop" "guban")
```

---

## 📎 常用命令速查

```bash
# ═══ 部署（統一使用 config-zip）═══
./scripts/deploy-app.sh mrvshop staging              # 部署 Staging
./scripts/deploy-app.sh mrvshop production            # 部署 Production

# ═══ 重啟 ═══
az functionapp restart --name mrvshop-func-staging -g rg-mrvshop-staging
az functionapp restart --name mrvshop-func         -g rg-mrvshop-prod

# ═══ 監控 ═══
func azure functionapp logstream mrvshop-func-staging  # Staging log
func azure functionapp logstream mrvshop-func          # Production log
az resource list -g rg-mrvshop-staging -o table        # Staging 資源
az resource list -g rg-mrvshop-prod -o table           # Production 資源

# ═══ 設定 ═══
az functionapp config appsettings list --name mrvshop-func         -g rg-mrvshop-prod -o table
az functionapp config appsettings list --name mrvshop-func-staging -g rg-mrvshop-staging -o table

# ═══ Bicep ═══
az bicep build --file infra/main.bicep
az deployment group what-if -g rg-mrvshop-staging --template-file infra/main.bicep \
  --parameters infra/main.parameters.mrvshop.json --parameters environment=staging
az deployment group what-if -g rg-mrvshop-prod --template-file infra/main.bicep \
  --parameters infra/main.parameters.mrvshop.json --parameters environment=production
```

---

## 📚 參考資源

- [Azure Functions 文件](https://learn.microsoft.com/azure/azure-functions/)
- [Bicep 語法參考](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [LINE Messaging API](https://developers.line.biz/en/docs/messaging-api/)
- [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/)

---

**文件版本**: 4.0.0
**最後更新**: 2026-02-22
