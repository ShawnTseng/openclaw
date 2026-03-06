# ✨ Buddy ShopAI 功能總覽

> 所有已上線與規劃中的功能，集中管理

---

## ✅ 已上線功能（v1.3.0）

### 核心 AI 客服

| 功能 | 說明 |
|------|------|
| LINE AI 智能客服 | Azure OpenAI gpt-4o-mini，自然語言回覆客戶問題 |
| 知識庫 JSON 配置 | `configs/{tenant}.json` 定義 FAQ、回答原則、商店資訊 |
| 對話記憶 | Azure Table Storage，保留最近 10 則訊息（5 輪對話） |
| 對話逾時自動重置 | 24 小時無互動自動清除歷史，開啟新對話 |
| 訊息防抖 | 3 秒合併視窗，連續多則訊息合併為一次 AI 呼叫 |
| Reply/Push Fallback | ReplyToken 過期時自動切換 Push Message，確保訊息送達 |

### 管理員功能

| 功能 | 說明 |
|------|------|
| LINE 指令介面 | 管理員在 LINE 輸入 `/manage` 指令即可操作（無需 API Key） |
| REST 管理 API | HTTP API 搭配 `X-Manage-Key` 認證，支援外部整合 |
| 真人/AI 模式切換 | 可將特定使用者切換為真人客服模式，AI 暫停回覆 |
| 重送 AI 回應 | `/manage retry {userId}` 重新觸發 AI 回覆 |
| 推播訊息 | `/manage push {userId} {msg}` 以 AI 身份發送自訂訊息 |
| 使用者狀態查詢 | 查詢活躍使用者、未回覆使用者、模式狀態 |
| 自動真人轉接 | AI 偵測需轉接時自動切換 + LINE 通知管理員（含客戶顯示名稱） |
| Quick Reply 快捷鍵 | 轉接通知附帶「切回 AI」「查看狀態」按鈕 |
| 24h 自動過期 | 真人模式超過 24 小時自動切回 AI，通知管理員 |

**管理員指令完整列表：**

```
/manage whoami           → 查看自己的 LINE userId
/manage status           → 系統狀態與版號
/manage unanswered       → 未收到 AI 回應的使用者
/manage users            → 最近 48h 活躍使用者
/manage modes            → 目前真人模式的使用者
/manage human {userId}   → 切換為真人客服模式
/manage ai {userId}      → 切換回 AI 模式
/manage ai last          → 將最後切換的客戶切回 AI
/manage retry {userId}   → 強制重送 AI 回應
/manage push {userId} {msg} → 以 AI 身份傳訊給使用者
/manage help             → 顯示說明
```

### 平台架構

| 功能 | 說明 |
|------|------|
| 多租戶架構 | Cell-based，每客戶獨立 Resource Group，完全隔離 |
| Infrastructure as Code | Bicep 模組化（Storage, Key Vault, OpenAI, Functions, App Insights） |
| CI/CD Pipeline | GitHub Actions：deploy, promote, provision, keep-warm（config-zip 部署） |
| Key Vault 密鑰管理 | Managed Identity + RBAC，零明文密鑰 |
| Flex Consumption | Production Always Ready 1 instance，零冷啟動 |

### 可靠性與監控

| 功能 | 說明 |
|------|------|
| LINE 簽章驗證 | HMAC-SHA256，阻擋偽造 Webhook 請求 |
| AI 呼叫重試 | Exponential Backoff（1s → 2s → 4s），處理 429 限流 |
| Table Storage 重試 | Polly Resilience Pipeline，3 次指數退避 |
| Health Check | `/api/health` 端點，檢查 Storage / OpenAI / LINE 配置 |
| Keep-Warm | Timer Trigger（5 分鐘）+ GitHub Actions 外部 ping（4 分鐘），僅 Staging |
| 使用量追蹤 | Application Insights 自訂指標：UserRequestsPerHour, OpenAIResponseTime |
| Application Insights | 自動收集 Requests / Dependencies / Exceptions / Traces |

---

## 📋 規劃中功能

### 🟡 P1：近期改善

| 功能 | 說明 | 狀態 |
|------|------|------|
| Google Sheets CMS | No-Code 知識庫管理，店家可自行編輯 FAQ | 規劃中 |
| FAQ 補充與優化 | 依客戶回饋持續擴充知識庫 | 持續進行 |
| Prompt 工程優化 | 精簡 System Prompt，降低 token 用量 | 規劃中 |

### 🟢 P2：功能擴展

| 功能 | 說明 | 狀態 |
|------|------|------|
| Instagram DM 支援 | Meta Graph API 整合，詳見 [INSTAGRAM_INTEGRATION.md](INSTAGRAM_INTEGRATION.md) | 設計中 |
| Rich Menu + Flex Message | LINE 圖文選單與互動訊息 | 規劃中 |
| 以圖搜圖 | GPT-4o Vision，客人傳圖片找相似商品 | 規劃中 |
| PDF 知識庫 RAG | 上傳 PDF 自動建立知識庫 | 規劃中 |
| 電商系統串接 | Shopify / 蝦皮 訂單查詢 | 規劃中 |

### 🔧 技術債務

| 項目 | 說明 |
|------|------|
| 單元測試 | 核心 Service 的自動化測試 |
| AppVersion 集中管理 | 目前 ManageApi.cs 和 ManageCommandService.cs 各自維護版號 |
| In-Memory 狀態 | `_rateLimitTracker` 在多實例時不共享 |

---

## 功能與程式碼對照

| 程式碼 | 功能 |
|--------|------|
| `LineWebhook.cs` | Webhook 入口、訊息處理、AI 回覆 |
| `ManageApi.cs` | 管理員 REST API |
| `ManageCommandService.cs` | 管理員 LINE 指令 |
| `HealthCheck.cs` | Health Check 端點 |
| `KeepWarmTimer.cs` | Keep-Warm Timer Trigger |
| `Services/ConversationHistoryService.cs` | 對話歷史、訊息防抖 |
| `Services/UserModeService.cs` | 真人/AI 模式切換 |
| `Services/PromptProvider.cs` | 多租戶知識庫載入 |
| `Services/LineSignatureValidator.cs` | Webhook 簽章驗證 |
| `configs/{tenant}.json` | 商店知識庫配置 |
| `infra/main.bicep` | 基礎設施定義 |

---

**最後更新**：2026-02-21
