# 🏗️ 系統架構總覽

> Buddy ShopAI 技術架構設計與原理

---

## 架構圖

```
┌─────────────────────────────────────────────────────────┐
│                    LINE Platform                        │
│              (Webhook POST Request)                     │
└────────────────────┬────────────────────────────────────┘
                     │ HTTPS
                     ▼
┌─────────────────────────────────────────────────────────┐
│            Azure Functions (Compute Layer)              │
│    .NET 8 Isolated Worker, Flex Consumption (Prod)     │
│                                                          │
│  ┌──────────────┐   ┌──────────────┐   ┌────────────┐  │
│  │LineWebhook.cs│──►│Services/     │──►│Models/     │  │
│  │(Entry Point) │   │- History     │   │- Entities  │  │
│  └──────────────┘   │- Signature   │   └────────────┘  │
│                     │- Prompt      │                    │
│                     └──────────────┘                    │
└────┬────────┬────────┬────────┬────────────────────────┘
     │        │        │        │
     │        │        │        └──────────────┐
     ▼        ▼        ▼                       ▼
┌─────────┐ ┌──────┐ ┌───────────────┐ ┌──────────────┐
│ Azure   │ │Table │ │   Key Vault   │ │Application   │
│ OpenAI  │ │Storage│ │  (Secrets)    │ │  Insights    │
│gpt-4o-  │ │(Conv.)│ │+ Managed ID   │ │ (Monitoring) │
│  mini   │ │       │ └───────────────┘ └──────────────┘
└─────────┘ └──────┘
```

---

## 多租戶架構（Cell-based）

每個客戶擁有**完全獨立**的 Azure Resource Group：

```
Tenant: mrvshop                    Tenant: guban
┌──────────────────────┐          ┌──────────────────────┐
│ rg-mrvshop-prod      │          │ rg-guban-prod        │
├──────────────────────┤          ├──────────────────────┤
│ mrvshop-func         │          │ guban-func           │
│ mrvshopt2icu7wp      │          │ guban{random}        │
│ kvmrvshopprodt2i     │          │ kvgubanprod{random}  │
│ mrvshop-openai-prod  │          │ guban-openai-prod    │
│ mrvshop-func (AI)    │          │ guban-func (AI)      │
└──────────────────────┘          └──────────────────────┘
```

### 優點

- ✅ **風險隔離** - 一個租戶故障不影響其他
- ✅ **帳務清晰** - 每個 RG 獨立帳單
- ✅ **客製彈性** - 可針對特定客戶調整
- ✅ **合規性** - 資料完全隔離

> 多租戶架構採用 Cell-based 模式，每個租戶的資源完全隔離，可獨立管理與擴展。

---

## 核心元件

### 1. Azure Functions（運算層）

| 屬性 | 值 |
|------|---|
| Runtime | .NET 8 (Isolated Worker) |
| Plan | Flex Consumption (Production) / Consumption (Staging) |
| OS | Linux |
| 觸發器 | HTTP Trigger, Timer Trigger |
| 認證 | Function Level |

**選擇理由**:
- Flex Consumption + Always Ready 1 instance (512 MB)，無冷啟動
- 成本僅 ~$5/月，遠低於 Premium Plan (~$158/月)
- Staging 維持 Consumption Plan 節省成本
- 自動擴展（最多 40 instances），應對流量波動
- 與 Azure 生態整合完美

> ⚠️ **注意**：`instanceMemoryMB` 必須設為 **512**，誤設為 2048 會導致 baseline 成本暴增 4 倍（~$5 → ~$21 USD/月）。

### 2. Azure OpenAI（AI 引擎）

| 屬性 | 值 |
|------|---|
| 模型 | gpt-4o-mini |
| SKU | Standard |
| TPM | 30,000 |
| 區域 | East US |

**選擇理由**:
- 比 GPT-3.5 更聰明且更便宜
- 128K Context Window
- 企業級 SLA

> 選用 gpt-4o-mini 在性能與成本間取得最佳平衡，128K Context Window 支援長對話。

### 3. Azure Table Storage（資料層）

| 用途 | Schema |
|------|--------|
| 對話歷史 | PartitionKey=userId, RowKey=timestamp |
| 每用戶上限 | 10 則訊息（5 輪對話） |
| 過期策略 | 24 小時無活動自動清除 |

**選擇理由**:
- 成本極低（$0.045/GB/月）
- 持久化，跨 instance 共享
- 簡單可靠

### 4. Azure Key Vault（安全層）

| 儲存項目 | 類型 |
|---------|------|
| LINE Channel Access Token | Secret |
| LINE Channel Secret | Secret |
| Azure OpenAI API Key | Secret |

**存取方式**: Managed Identity + RBAC

詳細說明：[安全架構](SECURITY.md)

---

## 訊息處理流程

```
1. Webhook Ingestion
   └─► 驗證 HMAC-SHA256 簽章

2. Usage Tracking
   └─► 記錄用戶使用頻率（Application Insights 指標）

3. Message Debounce
   └─► 3 秒內連續訊息合併

4. Context Loading
   ├─► 從 Table Storage 讀取對話歷史
   └─► 從 configs/{tenant}.json 讀取知識庫

5. AI Inference
   └─► 呼叫 Azure OpenAI (gpt-4o-mini)
       ├─► 包含重試機制（Exponential Backoff）
       └─► 處理 429 Rate Limit

6. Response Delivery
   └─► 透過 LINE Messaging API 回覆

7. State Persistence
   ├─► 寫入對話到 Table Storage
   └─► 異步清理超過上限的舊訊息
```

---

## 成本結構（每租戶）

| 服務 | 月成本 | 說明 |
|------|--------|------|
| Azure OpenAI | ~$2-3 | 200客/天×5問×30天 |
| Functions (Flex Prod) | ~$5 | Always Ready 1 instance |
| Functions (Staging) | $0 | Consumption 免費額度 |
| Storage | ~$0.01 | Table + Blob |
| Key Vault | ~$0.03 | 3 Secrets |
| App Insights | $0 | 免費 5GB/月 |
| **總計** | **~$7.50 USD** | **~240 TWD/月** |

詳細分析：[成本優化](COST_OPTIMIZATION.md)

---

## 技術決策

| 決策 | 選項 A | 選項 B | 選擇 | 理由 |
|------|--------|--------|------|------|
| AI 引擎 | Azure OpenAI | Google Gemini | A | 企業級、可控成本 |
| 對話儲存 | IMemoryCache | Table Storage | B | 持久化、跨 instance |
| 密鑰管理 | App Settings | Key Vault | B | 零明文風險 |
| 計算資源 | Dedicated Plan | Flex Consumption | B | 無冷啟動、低成本 (~$5/月) |
| 多租戶策略 | Shared RG | Isolated RG | B | 風險隔離、帳務清晰 |

詳細記錄：[經驗教訓](LESSONS_LEARNED.md)

---

## 擴展性設計

### 水平擴展
- Azure Functions Flex Consumption 自動擴展（最多 40 instances）
- Production Always Ready 1 instance，無冷啟動
- Table Storage 自動分區（PartitionKey = userId）

### 垂直擴展
- 可調整 Flex instance 記憶體（512MB / 2048MB / 4096MB）
- 可調整 OpenAI TPM 配額

### 區域擴展
- 支援多區域部署（目前 East US）
- 未來可考慮 Southeast Asia 降低延遲

---

## 監控與可觀測性

- **日誌**: Application Insights (Traces, Exceptions)
- **指標**: Requests, Duration, Dependencies
- **告警**: 可設定成本、錯誤率告警（待實作）
- **查詢**: KQL (Kusto Query Language)

詳細說明：[監控與維運](MONITORING.md)

---

## 下一步

- � [安全架構](SECURITY.md)
- 📊 [成本優化策略](COST_OPTIMIZATION.md)
- ⚙️ [配置管理](CONFIGURATION.md)
- 📋 [部署指南](DEPLOYMENT.md)

---

**架構負責人**: Shawn Tseng  
**最後更新**: 2026-02-21
