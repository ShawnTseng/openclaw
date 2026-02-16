# Buddy ShopAI — LINE 智慧客服平台 🤖

> 讓每個服飾品牌都能輕鬆擁有專屬、智慧、潮流的 AI 夥伴

服飾電商專用的 LINE AI 智慧客服平台，支援多租戶獨立部署。  
Azure Functions + Semantic Kernel + Azure OpenAI + LINE Messaging API。

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Azure Functions](https://img.shields.io/badge/Azure-Functions-0078D4)](https://azure.microsoft.com/services/functions/)

---

## ✨ 特色亮點

- ⚡ **Serverless 架構** - Consumption Plan，零固定成本
- 🤖 **AI 驅動對話** - Azure OpenAI gpt-4o-mini
- 👥 **多租戶支援** - 每個品牌獨立資源，完全隔離
- 💰 **超低成本** - 每租戶約 78 TWD/月
- 🔐 **企業級安全** - Key Vault + Managed Identity
- 📊 **對話記憶** - Table Storage 持久化，支援多輪對話
- 🚀 **一鍵部署** - Bicep IaC + 自動化腳本

---

## 🚀 快速開始

### 5 分鐘試用

```bash
# 1. Clone 專案
git clone https://github.com/ShawnTseng/88mrvShopAI.git
cd 88mrvShopAI

# 2. 設定環境
cp local.settings.json.example local.settings.json
# 編輯 local.settings.json 填入你的密鑰

# 3. 本地運行
dotnet restore && dotnet build
func start
```

詳細步驟: [快速開始指南](doc/getting-started/QUICKSTART.md)

### 部署到 Azure

```bash
# 1. 建立租戶設定
cp configs/_template.json configs/mytenant.json

# 2. 一鍵部署
./scripts/deploy-infra.sh mytenant
./scripts/deploy-app.sh mytenant
```

詳細步驟: [部署指南](doc/deployment/DEPLOYMENT_GUIDE.md)

---

## 📚 文檔導航

| 類別 | 文檔 | 說明 |
|------|------|------|
| **入門** | [快速開始](doc/getting-started/QUICKSTART.md) | 5分鐘快速上手 |
| | [本地開發](doc/getting-started/LOCAL_DEVELOPMENT.md) | 開發環境設定 |
| **部署** | [部署指南](doc/deployment/DEPLOYMENT_GUIDE.md) | 完整部署流程 |
| | [故障排除](doc/deployment/TROUBLESHOOTING.md) | 常見問題 |
| **架構** | [架構總覽](doc/architecture/OVERVIEW.md) | 系統設計概覽 |
| | [安全架構](doc/architecture/SECURITY.md) | 安全機制 |
| **指南** | [配置管理](doc/guides/CONFIGURATION.md) | 租戶配置 |
| | [監控維運](doc/guides/MONITORING.md) | 監控與日誌 |
| | [成本優化](doc/guides/COST_OPTIMIZATION.md) | 成本控制 |
| **商業** | [商業模式](doc/business/BUSINESS_MODEL.md) | 定位與市場 |
| | [定價策略](doc/business/PRICING.md) | 收費結構 |
| | [客戶上線](doc/business/ONBOARDING.md) | 新客戶 SOP |
| **開發** | [開發路線圖](doc/development/ROADMAP.md) | 功能規劃 |
| | [經驗教訓](doc/development/LESSONS_LEARNED.md) | 技術決策 |

> 📖 完整文檔索引: [doc/README.md](doc/README.md)

---

## 🏗️ 系統架構

```
LINE User
   │
   ▼
Azure Functions (.NET 8 Isolated)
   ├─► Azure OpenAI (gpt-4o-mini)
   ├─► Table Storage (對話歷史)
   ├─► Key Vault (密鑰管理)
   └─► Application Insights (監控)
```

### 多租戶架構

每個客戶獨立的 Azure Resource Group：

```
rg-mrvshop-prod/          rg-guban-prod/
├── mrvshop-func           ├── guban-func
├── mrvshopt2icu7wp        ├── guban{random}
├── kvmrvshopprodt2i       ├── kvgubanprod{random}
└── mrvshop-openai-prod    └── guban-openai-prod
```

詳細說明: [架構總覽](doc/architecture/OVERVIEW.md)

---

## 💰 成本估算

| 服務 | 月成本 | 說明 |
|------|--------|------|
| Azure OpenAI | ~$2-3 | gpt-4o-mini，200客/天×5問 |
| Functions | $0 | 免費額度 (1M 次/月) |
| Storage | ~$0.01 | Table + Blob |
| Key Vault | ~$0.03 | 3 Secrets |
| **總計** | **~$2.50 USD** | **~78 TWD/月** |

---

## 📦 功能清單

### ✅ 已實作

- ✅ AI 智慧對話（gpt-4o-mini）
- ✅ 多租戶架構（獨立 Resource Group）
- ✅ 對話記憶管理（Table Storage）
- ✅ 訊息防抖（3秒合併）
- ✅ 速率限制（10問/時）
- ✅ 對話逾時（24小時重置）
- ✅ 密鑰安全（Key Vault + Managed Identity）
- ✅ Webhook 簽章驗證（HMAC-SHA256）
- ✅ Bicep IaC（一鍵部署）
- ✅ 重試機制（Exponential Backoff）

### 📋 規劃中

| 優先級 | 功能 | 說明 |
|-------|------|------|
| P1 | Google Sheets CMS | 店家自助編輯 FAQ |
| P1 | 冷啟動優化 | UptimeRobot 或 Premium Plan |
| P2 | Instagram 支援 | 多平台客服 |
| P2 | Rich Menu | LINE 底部選單 |
| P2 | 以圖搜圖 | GPT-4o Vision |
| P3 | 電商串接 | Shopline/Cyberbiz API |

完整清單: [開發路線圖](doc/development/ROADMAP.md)

---

## 🛠️ 技術棧

| 類別 | 技術 | 版本 |
|------|------|------|
| Runtime | .NET | 8.0 |
| Framework | Azure Functions | v4 (Isolated) |
| AI | Microsoft.SemanticKernel | 1.70.0 |
| AI Model | Azure OpenAI | gpt-4o-mini |
| LINE SDK | Line.Messaging | 1.4.5 |
| Storage | Azure.Data.Tables | 12.11.0 |
| Security | Azure.Identity | latest |
| IaC | Bicep | latest |

---

## 📂 專案結構

```
BuddyShopAI/
├── Program.cs                         # DI 與服務註冊
├── LineWebhook.cs                     # LINE Webhook 處理
├── Services/
│   ├── ConversationHistoryService.cs  # 對話歷史管理
│   ├── LineSignatureValidator.cs      # 簽章驗證
│   └── PromptProvider.cs              # Prompt 與知識庫
├── configs/                           # 租戶配置
│   ├── _template.json
│   ├── mrvshop.json
│   └── guban.json
├── infra/                             # Bicep IaC
│   ├── main.bicep
│   ├── modules/
│   └── main.parameters.*.json
├── scripts/                           # 部署腳本
│   ├── deploy-infra.sh
│   ├── deploy-app.sh
│   └── deploy-all.sh
└── doc/                               # 完整文檔
```

---

## 🤝 貢獻

歡迎提交 Issue 或 Pull Request！

1. Fork 專案
2. 建立功能分支 (`git checkout -b feature/AmazingFeature`)
3. Commit 變更 (`git commit -m 'Add AmazingFeature'`)
4. Push 到分支 (`git push origin feature/AmazingFeature`)
5. 開啟 Pull Request

---

## 📄 授權

MIT License

## 👤 作者

**Shawn Tseng** - [GitHub](https://github.com/ShawnTseng)

## 🔗 相關連結

- [LINE Messaging API](https://developers.line.biz/en/docs/messaging-api/)
- [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure Functions](https://learn.microsoft.com/azure/azure-functions/)
- [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/)