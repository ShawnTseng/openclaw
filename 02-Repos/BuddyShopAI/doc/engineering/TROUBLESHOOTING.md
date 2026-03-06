# 🔧 故障排除

> 常見問題與解決方案

---

## ⚡ Quick Debug 流程圖

```
用戶回報「沒回應」
        │
        ▼
  curl /api/health
        │
   ┌────┴────┐
   │         │
 正常      異常
   │         │
   ▼         ▼
檢查 App    找到故障
Insights    的 check
   │         │
   ▼         ▼
搜尋最近    對照下方
的錯誤      問題表修復
```

---

## 部署相關

### Bicep 部署失敗

**問題**: `Resource 'Microsoft.CognitiveServices/accounts' not found`

**解決**:
```bash
# 確認區域支援 Azure OpenAI
az provider show -n Microsoft.CognitiveServices --query "resourceTypes[?resourceType=='accounts'].locations"

# 改用 East US
```

---

### VM 配額為零

**問題**: 無法建立 App Service Plan

**解決**: 使用 Consumption Plan（不需 VM 配額）

```bash
az functionapp create \
  --consumption-plan-location eastus \
  --name mrvshop-func
```

---

## Webhook 問題

### 完全沒回應

**診斷步驟**:
1. 檢查健康狀態: `curl https://mrvshop-func.azurewebsites.net/api/health`
2. 如果 health 也打不到 → Function App 可能停止了
3. 到 Azure Portal → Function App → Overview → 檢查 Status
4. 檢查 Application Insights → Failures

**常見原因**:
- Key Vault 存取失敗 → App 無法啟動 → 見 [Key Vault 存取問題](#key-vault-存取問題)
- Cold start 延遲 → 通常 2-5 秒後會恢復
- Consumption Plan 被回收 → 重新請求即可觸發冷啟動

### 401 Unauthorized

✅ **正常** - 表示簽章驗證正常運作（無 LINE signature header）

**如果正常請求也回 401**:
1. 檢查 App Insights → Custom Events → `SignatureValidationFailed`
2. 確認 LINE Channel Secret 正確: `az keyvault secret show --vault-name <vault> --name LINE-ChannelSecret`
3. 確認 LINE webhook URL 設定正確

### 500 Internal Server Error

檢查:
1. Health Check → 看哪個服務有問題
2. App Insights → Exceptions → 看 stack trace
3. Key Vault Secrets 是否正確
4. Managed Identity RBAC 是否授權

### 回應很慢（>10 秒）

**診斷**:
```kql
-- 在 App Insights → Logs 執行
customMetrics
| where name == "OpenAIResponseTime"
| where timestamp > ago(1h)
| summarize avg(value), percentile(value, 95) by bin(timestamp, 10m)
```

**原因與解決**:
| 原因 | 診斷 | 解決 |
|------|------|------|
| OpenAI rate limit (429) | App Insights 看到 retry warnings | 增加 TPM 配額 |
| Cold start | 第一次請求特別慢 | 考慮 Premium Plan |
| Table Storage 慢 | Dependencies duration > 1s | 已有 retry policy |

---

## Key Vault 存取問題

**問題**: `The user, group or application does not have secrets get permission`

**解決**:
```bash
# 授予 Managed Identity 權限
PRINCIPAL_ID=$(az functionapp show --name mrvshop-func --resource-group rg-mrvshop-prod --query "identity.principalId" -o tsv)

az role assignment create \
  --assignee "$PRINCIPAL_ID" \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/.../vaults/kvmrvshop...
```

**驗證**:
```bash
# 檢查 RBAC
az role assignment list --scope /subscriptions/.../vaults/kvmrvshop... --output table
```

---

## OpenAI 相關

### 429 Too Many Requests

已實作 Exponential Backoff 重試（1s → 2s → 4s，最多 3 次）。

**如果頻繁觸發**:
```kql
-- 查看 429 頻率
traces
| where message contains "Rate limit hit (429)"
| summarize count() by bin(timestamp, 1h)
```

**解決**: 在 Azure Portal 增加 OpenAI TPM 配額。

### Response 緩慢

```kql
-- 查看 OpenAI 回應時間分佈
customMetrics
| where name == "OpenAIResponseTime"
| where customDimensions.success == "true"
| summarize percentile(value, 50), percentile(value, 95), percentile(value, 99) by bin(timestamp, 1h)
```

### AI 回應內容異常

1. 檢查 `configs/mrvshop.json` system prompt
2. 確認 `AzureOpenAI:DeploymentName` 設定正確
3. Health Check → `azureOpenAI` 的 `details.deploymentName`

---

## Table Storage 相關

### 對話歷史遺失

**已有自動保護**: Polly retry policy (3 次重試, exponential backoff)

**診斷**:
```kql
traces
| where message contains "Retrying Table Storage"
| where timestamp > ago(1h)
| summarize count() by bin(timestamp, 10m)
```

**如果 retry 後仍失敗**:
```kql
traces
| where message contains "Failed to save" or message contains "after retries"
| where timestamp > ago(1h)
```

---

## Rate Limiting 相關

### 用戶反映被限制

**設計行為**: 每用戶每小時最多 10 個問題。

**查看被限制的用戶**:
```kql
customEvents
| where name == "RateLimitExceeded"
| where timestamp > ago(24h)
| summarize count() by tostring(customDimensions.userId)
| order by count_ desc
```

**調整**: 使用量追蹤已改為 Application Insights 指標（`UserRequestsPerHour`），不再限制付費客戶使用。

---

## 本地開發

### Azurite 連接失敗

```bash
# 清除並重啟
rm -rf ~/.azurite
mkdir ~/.azurite
azurite --silent --location ~/.azurite
```

### Port 7071 被佔用

```bash
lsof -i :7071
kill -9 <PID>
```

---

需要更多協助? [開 Issue](https://github.com/ShawnTseng/BuddyShopAI/issues)
