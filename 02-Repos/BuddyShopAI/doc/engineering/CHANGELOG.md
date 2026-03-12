# 📝 變更歷史

> Buddy ShopAI 版本更新記錄

---

## v1.4.0 - 2026-03-06

### ✨ 功能

- 支援多組管理員 LINE UserId（`Manage__LineUserIds`，逗號分隔）

### 🏗️ 架構

- `ManageCommandService`：`_manageLineUserId`（單一字串）改為 `_manageLineUserIds`（`HashSet<string>`），`IsManager()` 使用 `Contains()` 判斷
- `Program.cs`：讀取 `Manage:LineUserIds` 並以逗號分割
- 新增 `scripts/deploy-settings.sh`：將 `local.settings.json` 同步到 Azure 各環境
- Bicep 修正 `instanceMemoryMB` 2048 → 512，Production 月成本降回 ~$7.50 USD（原誤設導致 ~$27 USD/月）

### 📚 文檔

- 所有文件 `Manage__LineUserId` 更名為 `Manage__LineUserIds`
- DEPLOYMENT.md 新增 `deploy-settings.sh` 使用說明
- COST_OPTIMIZATION.md 新增 Always Ready instanceMemoryMB 成本對照表

---

## v1.3.0 - 2026-02-22

### ✨ 功能

- 新增 LINE Quick Reply 按鈕（真人轉接通知附帶「切回 AI」「查看狀態」快捷鍵）
- 新增 `/manage ai last` 捷徑（一鍵將最後切換的客戶切回 AI 模式）
- 新增 24 小時真人模式自動過期（HumanModeTimeoutTimer，每 30 分鐘檢查）
- 強化轉接通知：顯示客戶 LINE 顯示名稱（透過 LINE Profile API）

### 🏗️ 架構

- CI/CD 統一使用 `config-zip` 部署（移除 blob SAS / func publish 等不可靠方式）
- Staging 部署前自動刪除 `WEBSITE_RUN_FROM_PACKAGE` 避免衝突
- Health check 重試從 15 次降為 5 次（20 秒間隔，總計約 2 分鐘）
- 修正 promote.yml / keep-warm.yml curl 比較語法（`-eq` → `= "200"` + `|| echo "000"`）

### 🧹 代碼清理

- 移除所有 C# 原始碼的 XML doc / 行內註解 / #region
- 重寫全部 4 個 GitHub Actions workflow（deploy / promote / keep-warm / provision）
- 精簡部署文件，移除 `func azure functionapp publish` 參考

---

## v1.2.0 - 2026-02-21

### ✨ 功能

- Production 升級為 Flex Consumption Plan + Always Ready，徹底消除冷啟動
- 新增自動真人轉接：AI 回覆含「轉接專人」時自動切換為真人模式並通知管理員
- 新增斷貨改款物流說明 FAQ（二次補寄、運費店家負擔）
- 新增未收到完整商品 FAQ（引導提供姓名電話查詢）
- 新增付款相關問題 FAQ
- Config 新增自動轉接與真人模式說明的 responseGuidelines

### 🏗️ 架構

- Infra: Production 從 Consumption Plan 升級為 Flex Consumption (FC1)
- Infra: Staging 維持 Consumption Plan（節省成本）
- Bicep: functionApp.bicep 支援條件式 Flex / Consumption 部署
- Bicep: 新增 deploymentpackage blob container（Flex 必要）
- GitHub Actions: deploy.yml Production 改用 `az functionapp deploy --type zip`
- GitHub Actions: keep-warm.yml 改為 4 分鐘、僅 Staging（Flex 不需 keep-warm）

### 📚 文檔

- 尺寸建議功能移至 Phase 2 規劃
- 更新報價暨服務範圍說明書（新增真人/AI 切換功能說明）
- 更新成本結構文件（Flex ~$7.50/月）
- 更新架構文件（Flex + Always Ready）
- 更新部署指南（Flex 部署方式）

---

## v1.1.0 - 2026-02-17

### ✨ 功能

- 新增真人 / AI 客服模式切換（/manage human、/manage ai）
- 新增 LINE 管理員指令介面（/manage 系列指令）
- 新增 ManageApi HTTP 端點（狀態查詢、模式切換、重送回應）
- 移除硬性速率限制，改用 Application Insights 追蹤

---

## v1.0.0 - 2026-02-13

### ✨ 功能

- 多租戶平台化架構
- Azure OpenAI gpt-4o-mini 整合
- 對話歷史持久化（Table Storage）
- 訊息防抖（3秒合併）
- 速率限制（10問/時）
- 對話逾時（24小時重置）
- Key Vault + Managed Identity
- Webhook 簽章驗證
- Bicep IaC 自動化部署
- 一鍵部署腳本

### 🏗️ 架構

- Cell-based 多租戶架構
- Consumption Plan（零固定成本）
- 每租戶獨立 Resource Group

### 📚 文檔

- 完整重構文檔結構（SOLID 原則）
- 8 個類別，20+ 文檔
- 中英文導航索引

---

## v0.3 - 2026-02-12

- Azure OpenAI 取代 Google Gemini
- 新增對話逾時與速率限制
- 優化 Prompt 結構

---

## v0.2 - 2026-02-11

- 對話歷史 (Table Storage)
- 訊息防抖機制
- 重試機制 (Exponential Backoff)

---

## v0.1 - 初始版本

- LINE Bot 基礎功能
- Google Gemini 整合
- IMemoryCache 對話記憶

---

**維護者**: Shawn Tseng
