# 📊 監控與維運

> Application Insights 監控、日誌分析與維運指南

---

## Quick Debug 工作流程

> 當 API 沒回應時，按照以下步驟快速定位問題

### Step 1: 健康檢查

```bash
# 快速確認所有服務狀態
curl https://mrvshop-func.azurewebsites.net/api/health | jq .

# 預期回應:
# {
#   "status": "healthy",
#   "timestamp": "2026-02-13T10:00:00Z",
#   "checks": {
#     "tableStorage": { "status": "healthy" },
#     "azureOpenAI": { "status": "healthy", "details": {...} },
#     "lineMessaging": { "status": "healthy" }
#   }
# }
```

| Status | 含義 | 行動 |
|--------|------|------|
| `healthy` | 所有服務正常 | 問題可能在 LINE 端或網路 |
| `degraded` | 部分服務異常 | 檢查 `checks` 找出哪個服務壞了 |

### Step 2: 查看 Application Insights

```
Azure Portal → Application Insights (mrvshop-appinsights)
→ Investigate → Failures
→ 查看最近的錯誤
```

### Step 3: 用 operationId 追蹤請求

每個 webhook 請求都會產生唯一的 `operationId`。  
在 Application Insights → Logs 執行：

```kql
union traces, exceptions, customEvents, customMetrics
| where customDimensions.operationId == "你的operationId"
| order by timestamp asc
```

### Step 4: 對照常見問題表

| 症狀 | 可能原因 | 診斷方式 | 修復 |
|------|---------|---------|------|
| 完全沒回應 | Function App 沒啟動 | Health Check 打不到 | 檢查 Azure Portal Function App 狀態 |
| 偶爾沒回應 | Cold start | App Insights → Performance | 考慮 Premium Plan 或 warm-up |
| 回應慢 | OpenAI rate limit | App Insights → `OpenAIResponseTime` metric | 增加 TPM 配額 |
| 401 錯誤 | LINE signature 失敗 | App Insights → `SignatureValidationFailed` event | 檢查 LINE Channel Secret |
| 用戶被拒 | Rate limiting | App Insights → `RateLimitExceeded` event | 調整 `_maxQuestionsPerHour` |
| 歷史訊息遺失 | Table Storage 錯誤 | App Insights → Dependencies → failures | 已有 retry policy 自動處理 |

---

## Application Insights

### 設定說明

| 設定 | 值 | 說明 |
|------|-----|------|
| Sampling | 90% | 節省成本，每月 ~500MB 資料量 |
| Retention | 30 天 | 免費保留 30 天 |
| maxTelemetryItemsPerSecond | 5 | 限制每秒最多 5 筆 telemetry |

### 自動收集的遙測

| 類型 | 說明 |
|------|------|
| Requests | HTTP 請求（成功/失敗、延遲） |
| Dependencies | Azure OpenAI、Table Storage 呼叫 |
| Exceptions | 未處理的例外 |
| Traces | ILogger 日誌輸出 |

### 自訂遙測事件

| Event Name | 觸發時機 | Properties |
|-----------|---------|------------|
| `SignatureValidationFailed` | LINE signature 驗證失敗 | `operationId` |
| `RateLimitExceeded` | 用戶超過速率限制 | `operationId`, `userId` |
| `OpenAIRequestStart` | 開始呼叫 OpenAI | `operationId`, `userId` |

### 自訂指標

| Metric Name | 說明 | Properties |
|------------|------|------------|
| `OpenAIResponseTime` | OpenAI API 回應時間 (ms) | `operationId`, `userId`, `success` |

### 查看即時日誌

```bash
# 使用 Azure CLI
func azure functionapp logstream ${TENANT_ID}-func

# 或在 Azure Portal
Function App → Log stream
```

---

## KQL 查詢範例

### 查看最近的錯誤

```kql
traces
| where severityLevel >= 3
| where timestamp > ago(1h)
| project timestamp, message, severityLevel
| order by timestamp desc
```

### 用 operationId 追蹤完整請求流程

```kql
union traces, exceptions, customEvents, customMetrics
| where customDimensions.operationId == "<operationId>"
| project timestamp, itemType, 
    message = coalesce(message, name, tostring(customDimensions)),
    severityLevel
| order by timestamp asc
```

### 查看 OpenAI 回應時間趨勢

```kql
customMetrics
| where name == "OpenAIResponseTime"
| summarize avg(value), percentile(value, 95), max(value) by bin(timestamp, 1h)
| render timechart
```

### 查看失敗的 OpenAI 呼叫

```kql
customMetrics
| where name == "OpenAIResponseTime"
| where customDimensions.success == "false"
| project timestamp, value, customDimensions.operationId, customDimensions.userId
| order by timestamp desc
```

### 查看 OpenAI 呼叫統計

```kql
dependencies
| where target contains "openai"
| summarize count(), avg(duration) by bin(timestamp, 1h)
| render timechart
```

### 查看用戶活躍度

```kql
traces
| where message contains "User"
| summarize count() by bin(timestamp, 1h)
| render timechart
```

### 查看 Rate Limiting 事件

```kql
customEvents
| where name == "RateLimitExceeded"
| summarize count() by bin(timestamp, 1h), tostring(customDimensions.userId)
| render timechart
```

### 查看 Table Storage Retry 情況

```kql
traces
| where message contains "Retrying Table Storage"
| summarize count() by bin(timestamp, 1h)
| render timechart
```

### 查看 Signature 驗證失敗

```kql
customEvents
| where name == "SignatureValidationFailed"
| summarize count() by bin(timestamp, 1h)
| render timechart
```

---

## Retry Policy 說明

### OpenAI (429 Rate Limit)
- **策略**: Exponential Backoff
- **最大重試**: 3 次
- **延遲**: 1s → 2s → 4s
- **觸發條件**: HTTP 429 錯誤
- **位置**: `LineWebhook.cs` → `GetAIResponseWithRetryAsync()`

### Table Storage (暫時性錯誤)
- **策略**: Exponential Backoff (Polly)
- **最大重試**: 3 次
- **延遲**: 1s → 2s → 4s
- **觸發條件**: 任何 Table Storage 例外
- **位置**: `ConversationHistoryService.cs` → `SaveMessageAsync()`

---

## 成本監控

### Application Insights 成本

| 項目 | 免費額度 | 預估使用量 |
|------|---------|-----------|
| 資料擷取 | 5 GB/月 | ~500 MB/月 (90% sampling) |
| 資料保留 | 90 天免費 | 設定 30 天 |
| Alert 規則 | 免費 | 視需要新增 |

```bash
# 查看 Resource Group 成本
az consumption usage list \
  --resource-group rg-${TENANT_ID}-prod \
  --start-date 2026-02-01 \
  --end-date 2026-02-28
```

或使用 Azure Portal → Cost Management

---

## 健康檢查端點

### GET /api/health

自動檢查以下服務：

| 檢查項目 | 檢查方式 | 失敗影響 |
|---------|---------|---------|
| Table Storage | 建立/存取 ConversationHistory table | 對話歷史無法存取 |
| Azure OpenAI | 驗證 Endpoint 和 DeploymentName 配置 | AI 無法回應 |
| LINE Messaging | 驗證 ChannelAccessToken 和 ChannelSecret | 無法接收/發送訊息 |

### 設定自動監控

使用 UptimeRobot（免費方案）每 5 分鐘 ping `/api/health`：
1. 建立 [UptimeRobot](https://uptimerobot.com) 帳號
2. 新增 Monitor → HTTP(s) → `https://mrvshop-func.azurewebsites.net/api/health`
3. 設定 Alert Contact（Email）

---

## Correlation ID 追蹤

每個 webhook 請求都會自動產生一個 `operationId` (GUID)，串聯整個處理流程：

```
Webhook Request → Signature Validation → Rate Limit Check → 
Message Debounce → Load History → OpenAI Call → Save Response → LINE Reply
```

所有 log 和 telemetry 都包含 `operationId`，可以在 Application Insights 中：
1. 找到任一筆 log → 看到 `OperationId`
2. 用 KQL 查詢該 `operationId` 的所有相關事件
3. 完整重建請求的處理流程

---

詳細成本策略: [成本優化](COST_OPTIMIZATION.md)
