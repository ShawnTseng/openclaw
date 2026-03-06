# Buddy ShopAI — LINE 智慧客服平台 🤖

> 讓每個服飾品牌都能輕鬆擁有專屬、智慧、潮流的 AI 夥伴

服飾電商專用的 LINE AI 智慧客服平台，支援多租戶獨立部署。  
Azure Functions + Semantic Kernel + Azure OpenAI + LINE Messaging API。

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Azure Functions](https://img.shields.io/badge/Azure-Functions-0078D4)](https://azure.microsoft.com/services/functions/)

---

## ✨ 特色亮點

- ⚡ **Serverless 架構** - Flex Consumption (Production) + Consumption Plan (Staging)
- 🤖 **AI 驅動對話** - Azure OpenAI gpt-4o-mini
- 👥 **多租戶支援** - 每個品牌獨立資源，完全隔離
- 💰 **超低成本** - 每租戶約 250 TWD/月（含零冷啟動）
- 🔐 **企業級安全** - Key Vault + Managed Identity
- 📊 **對話記憶** - Table Storage 持久化，支援多輪對話
- 🚀 **一鍵部署** - Bicep IaC + 自動化腳本

---

## 🚀 快速開始

### 5 分鐘試用

```bash
# 1. Clone 專案
git clone https://github.com/ShawnTseng/BuddyShopAI.git
cd BuddyShopAI

# 2. 設定環境
cp local.settings.json.example local.settings.json
# 編輯 local.settings.json 填入你的密鑰

# 3. 本地運行
dotnet restore && dotnet build
func start
```

詳細步驟: [快速開始指南](doc/engineering/QUICKSTART.md)

### 部署到 Azure

```bash
# 1. 建立租戶設定
cp configs/_template.json configs/mytenant.json

# 2. 一鍵部署
./scripts/deploy-infra.sh mytenant
./scripts/deploy-app.sh mytenant
```

詳細步驟: [部署指南](doc/engineering/DEPLOYMENT.md)

---

## 📚 文檔導航

| 類別 | 文檔 | 說明 |
|------|------|------|
| **入門** | [快速開始](doc/engineering/QUICKSTART.md) | 5分鐘快速上手 |
| | [本地開發](doc/engineering/LOCAL_DEVELOPMENT.md) | 開發環境設定 |
| **部署** | [部署指南](doc/engineering/DEPLOYMENT.md) | 完整部署流程 |
| | [故障排除](doc/engineering/TROUBLESHOOTING.md) | 常見問題 |
| **架構** | [架構總覽](doc/engineering/ARCHITECTURE.md) | 系統設計概覽 |
| | [安全架構](doc/engineering/SECURITY.md) | 安全機制 |
| **指南** | [配置管理](doc/engineering/CONFIGURATION.md) | 租戶配置 |
| | [監控維運](doc/engineering/MONITORING.md) | 監控與日誌 |
| | [成本優化](doc/engineering/COST_OPTIMIZATION.md) | 成本控制 |
| **功能** | [功能總覽](doc/engineering/FEATURES.md) | 所有功能清單 |
| **商業** | [商業模式](doc/business/BUSINESS_MODEL.md) | 定位與市場 |
| | [定價策略](doc/business/PRICING.md) | 收費結構 |
| | [客戶上線](doc/business/ONBOARDING.md) | 新客戶 SOP |

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

詳細說明: [架構總覽](doc/engineering/ARCHITECTURE.md)

---

## 💰 成本估算

| 服務 | Production | Staging | 說明 |
|------|-----------|---------|------|
| Azure OpenAI | ~$2-3 | ~$0.50 | gpt-4o-mini |
| Functions | ~$5 | $0 | Flex Always Ready / 免費額度 |
| Storage | ~$0.01 | ~$0.01 | Table + Blob |
| Key Vault | ~$0.03 | ~$0.03 | Secret 操作 |
| **合計** | **~$7.50** | **~$0.55** | **~$8 USD/月** |

---

## 📦 功能清單

### ✅ 已實作

- ✅ AI 智慧對話（gpt-4o-mini）
- ✅ 多租戶架構（獨立 Resource Group）
- ✅ 對話記憶管理（Table Storage）
- ✅ 訊息防抖（3秒合併）
- ✅ 使用量追蹤（Application Insights 指標）
- ✅ 對話逎時（24小時重置）
- ✅ 密鑰安全（Key Vault + Managed Identity）
- ✅ Webhook 簽章驗證（HMAC-SHA256）
- ✅ Bicep IaC（一鍵部署）
- ✅ 重試機制（Exponential Backoff）
- ✅ 真人/AI 客服模式切換
- ✅ 管理員 LINE 指令 + REST API（含 Quick Reply 快捷鍵）
- ✅ 自動真人轉接（AI 偵測需轉接時自動切換 + 通知管理員）
- ✅ 24 小時真人模式自動過期
- ✅ Flex Consumption (Production) — Always Ready 零冷啟動
- ✅ Keep-Warm 防冷啟動（Staging）
- ✅ CI/CD Pipeline（GitHub Actions，config-zip 部署）

### 📋 規劃中

| 優先級 | 功能 | 說明 |
|-------|------|------|
| P1 | Google Sheets CMS | 店家自助編輯 FAQ |
| P2 | Instagram 支援 | 多平台客服 |
| P2 | Rich Menu | LINE 底部選單 |
| P2 | 以圖搜圖 | GPT-4o Vision |
| P3 | 電商串接 | Shopline/Cyberbiz API |

完整清單: [功能總覽](doc/engineering/FEATURES.md)

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
├── ManageApi.cs                       # 管理員 REST API
├── HealthCheck.cs                     # Health Check 端點
├── KeepWarmTimer.cs                   # Keep-Warm Timer
├── HumanModeTimeoutTimer.cs           # 24h 真人模式自動過期
├── Models/
│   ├── ConversationMessageEntity.cs   # 對話歷史 Entity
│   ├── PendingMessageEntity.cs        # 訊息合併暫存 Entity
│   └── UserModeEntity.cs              # 用戶模式 Entity
├── Services/
│   ├── ConversationHistoryService.cs  # 對話歷史管理
│   ├── ManageCommandService.cs        # 管理員 LINE 指令
│   ├── UserModeService.cs             # 真人/AI 模式切換
│   ├── LineSignatureValidator.cs      # 簽章驗證
│   └── PromptProvider.cs              # 多租戶知識庫
├── configs/                           # 租戶配置
├── infra/                             # Bicep IaC
├── scripts/                           # 部署腳本
├── .github/workflows/                 # CI/CD
│   ├── deploy.yml                     # 部署應用
│   ├── provision.yml                  # 佈建基礎設施
│   ├── promote.yml                    # Staging → Production
│   └── keep-warm.yml                  # 防冷啟動
└── doc/
    ├── engineering/                   # 工程技術文檔
    └── business/                      # 商業文檔
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