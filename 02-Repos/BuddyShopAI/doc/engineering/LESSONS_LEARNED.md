# Buddy ShopAI 架構演進與經驗教訓

> 最後更新：2026-02-13  
> 版本：v1.0 (Consumption Plan 最低成本架構)

本文記錄專案從初始概念到生產部署的完整歷程，包含失敗經驗與關鍵決策。

---

## 📋 目錄

1. [架構演進歷程](#架構演進歷程)
2. [重要經驗教訓](#重要經驗教訓)
3. [技術決策記錄](#技術決策記錄)
4. [成本優化策略](#成本優化策略)

---

## 架構演進歷程

### 第一階段：初始架構 (v0.1)

**技術棧**：Azure Functions + Google Gemini + IMemoryCache

**問題**：
- Gemini 免費額度有限，正式營運不可靠
- IMemoryCache 在 Consumption Plan 下會隨 instance 回收而遺失
- 無密鑰管理（API Key 直接放 App Settings）

### 第二階段：Azure OpenAI + Table Storage (v0.2 ~ v0.3)

**改進**：
- 切換到 Azure OpenAI (gpt-4o-mini) — 按用量計費，可控
- 對話歷史改用 Azure Table Storage — 持久化、跨 instance 共享
- 新增 Key Vault + Managed Identity — 零明文密鑰
- 新增對話逾時 (24h) 與速率限制 (10問/時)

### 第三階段：部署失敗與突破 (v1.0)

#### 🔴 失敗 1：Japan East 不支援 gpt-4o-mini Standard SKU
- 嘗試在離台灣最近的 Japan East 部署 → 模型 SKU 不支援
- **解決**：改用 East US（支援所有模型 SKU）

#### 🔴 失敗 2：訂閱 VM 配額為零
- 嘗試 Y1 Dynamic, B1 Basic, EP1 ElasticPremium → 全部因 VM 配額問題失敗
- 診斷：`az vm list-usage --location <any-region>` 全部返回空陣列
- **原因**：新訂閱的 VM 配額可能為 0

#### 🔴 失敗 3：Container Apps 需要 Docker
- Container Apps Environment 可以繞過 VM 配額
- 但 Functions on Container Apps 不支援 zip deploy，需要 Docker image
- 需要 Azure Container Registry (~$5/月)，增加成本和複雜度

#### ✅ 突破：Consumption Plan 共享 Dynamic Plan
- 直接用 `az functionapp create --consumption-plan-location eastus` 
- Azure 自動建立 `EastUSLinuxDynamicPlan`（共享、零固定成本）
- **完全繞過 VM 配額限制**
- 支援標準 `func azure functionapp publish` 部署

---

## 重要經驗教訓

### 1. 訂閱配額的隱形陷阱

> 新 Azure 訂閱可能有極低（甚至為 0）的 VM 配額。

**診斷方法**：
\`\`\`bash
az vm list-usage --location eastus --query "[?limit > '0']"
# 如果返回 []，代表該訂閱無 VM 配額
\`\`\`

**繞過策略**：使用不依賴 VM 配額的 Serverless 服務（Consumption Plan Functions）。

### 2. Azure Functions 部署模式選擇

| 模式 | 成本 | 配額需求 | 適用場景 |
|-----|------|---------|---------|
| **Consumption Plan** ✅ | $0 固定 + 用量 | **不需要** VM 配額 | 低/中流量、成本敏感 |
| Premium Plan | ~$150/月起 | 需要 VM 配額 | VNet、長時間執行 |
| Container Apps | ~$0 + 用量 | 需要 VM 配額 | Custom container |
| Dedicated (App Service) | ~$13/月起 | 需要 VM 配額 | 持續運行需求 |

**結論**：小專案首選 Consumption Plan。

### 3. 不要過度工程化

**實際經驗**：
- ❌ 三環境 (dev/staging/prod) → 只需 prod
- ❌ GitHub Actions CI/CD → 手動 `func publish` 足夠
- ❌ 複雜的 Alert 系統 → App Insights 免費額度已足夠
- ❌ Log Analytics Workspace → App Insights 預設即可
- ❌ Container Apps → 標準 Consumption Plan 更簡單

**原則**：先做最簡單能用的，有需要再複雜化。

### 4. Managed Identity > 明文密鑰

- **永遠使用 Managed Identity** + Key Vault References
- 程式碼裡不該出現任何 API Key 明文
- RBAC 最小權限：只給 `Key Vault Secrets User`，不給 `Owner`

### 5. Bicep vs CLI 的取捨

| 適合 Bicep | 適合 CLI |
|-----------|---------|
| 需要重複建立的資源 | 一次性設定 |
| 需要版本控制 | 簡單操作 |
| 核心基礎設施 | RBAC 授權 |
| Storage, Key Vault, OpenAI | Function App (Consumption) |

**本專案選擇**：
- Bicep → Storage, Key Vault, OpenAI, Function App（核心資源）
- CLI → RBAC 授權、App Settings（一次性設定）

### 6. 區域選擇的現實

| 區域 | 離台灣距離 | gpt-4o-mini 支援 | 備註 |
|------|-----------|-----------------|------|
| Japan East | 最近 ~2,100km | ❌ Standard SKU 不支援 | 延遲最低但無法使用 |
| Southeast Asia | 近 ~2,500km | 需確認 | 潛在替代方案 |
| **East US** | 遠 ~12,000km | ✅ 完整支援 | **目前使用** |

**待辦**：確認 Southeast Asia 或其他亞洲區域是否支援 gpt-4o-mini，以降低延遲。

---

## 技術決策記錄

### ADR-001: AI 引擎選擇

**決策**：Azure OpenAI (gpt-4o-mini) 取代 Google Gemini  
**原因**：
- Gemini 免費額度有限，不適合正式營運
- Azure OpenAI 按用量計費，成本可控
- 與 Azure 生態整合更好（Key Vault、Managed Identity）
- Semantic Kernel 對 Azure OpenAI 支援最完整

### ADR-002: 對話歷史儲存

**決策**：Azure Table Storage 取代 IMemoryCache  
**原因**：
- IMemoryCache 在 Serverless 環境下不可靠（instance 回收即遺失）
- Table Storage 成本極低（$0.045/GB/月）
- 支援 PartitionKey (userId) 查詢，效能足夠
- 與 AzureWebJobsStorage 共用帳號，不增加資源

### ADR-003: 商店知識庫管理

**決策**：靜態 JSON (`store-config.json`) 作為知識庫  
**原因**：Phase 1 快速上線，開發成本最低  
**待改進**：未來升級為 Google Sheets No-Code CMS（見 [ROADMAP.md](ROADMAP.md)）

### ADR-004: 密鑰管理

**決策**：Key Vault + Managed Identity + RBAC  
**原因**：
- 零明文密鑰暴露風險
- Managed Identity 無需管理 credential rotation
- RBAC 比 Access Policy 更精細可控

---

## 成本優化策略

### 已實施

1. **Consumption Plan**：零固定成本，只按用量計費
2. **gpt-4o-mini**：Input $0.15/1M tokens（比 gpt-4o 便宜 33 倍）
3. **訊息防抖**：3 秒合併，減少 40-60% AI 呼叫
4. **對話歷史上限**：10 則（5 輪），控制每次 token 消耗
5. **速率限制**：10 問/時，防止單用戶濫用
6. **免費額度最大化**：
   - Functions: 1M executions/月 免費
   - App Insights: 5GB/月 免費
   - Key Vault: 前 10K transactions 免費

### 月費明細

| 項目 | 估算 |
|------|------|
| OpenAI (200客×5問×30天) | ~$2.40 |
| Storage (10MB Table) | ~$0.01 |
| Key Vault (3 secrets) | ~$0.09 |
| Functions (30K 次) | $0 (免費額度) |
| App Insights | $0 (免費額度) |
| **總計** | **~$2.50 USD/月 (~78 TWD)** |

---

## 變更歷史

| 版本 | 日期 | 變更 |
|-----|------|------|
| v1.0 | 2026-02-13 | Consumption Plan 架構、最低成本營運、完整知識庫 |
| v0.3 | 2026-02-12 | Azure OpenAI、對話逾時、速率限制 |
| v0.2 | 2026-02-11 | 對話歷史 (Table Storage)、訊息防抖 |
| v0.1 | 初始版本 | LINE Bot + Google Gemini + IMemoryCache |

---

**維護者**：Shawn Tseng (shawntseng40@gmail.com) 