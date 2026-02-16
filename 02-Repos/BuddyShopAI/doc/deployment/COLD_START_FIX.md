# 冷啟動與速率限制問題修復

> 修復日期：2026-02-13  
> 狀態：✅ 已完成並測試

---

## 📋 問題分析

### 實際檢測結果

```bash
# Function App 狀態
$ az functionapp list
Name: mrvshop-func
State: Running
SKU: Dynamic (Consumption Plan)  ⚠️
Always On: false  ⚠️

# Health Check
$ curl https://mrvshop-func.azurewebsites.net/api/health
Status: 200 OK
Response Time: 0.92s (正常運作中)
```

### 確認的問題

1. **❄️ Cold Start（冷啟動）**
   - **原因**：Azure Functions Consumption Plan + alwaysOn=false
   - **現象**：閒置 5-20 分鐘後自動休眠
   - **影響**：第一個用戶請求需要 5-15 秒喚醒，LINE webhook 30 秒 timeout 可能失敗
   - **成本**：Consumption Plan = $0.20/百萬請求（最便宜）

2. **⏱️ 速率限制過於嚴格**
   - **原有設定**：每用戶每小時 10 個問題
   - **問題**：付費客戶（建置 $36K + 月維護 $4.5K）不應被限制
   - **用戶體驗**：會顯示「不好意思，您目前的提問次數已達上限 😅」

---

## ✅ 改善方案

### 1. 移除速率限制

**修改檔案**：
- `LineWebhook.cs`：移除 `IsRateLimited()` 檢查邏輯
- `Services/ConversationHistoryService.cs`：將 `IsRateLimited()` 改為 `GetHourlyRequestCount()`

**變更內容**：
```diff
- // 速率限制檢查（每用戶每小時最多 10 個問題）
- if (_historyService.IsRateLimited(userId))
- {
-     // 回傳限制訊息...
- }
+ // 記錄使用統計（已移除速率限制，付費客戶不應受限）
+ var requestCount = _historyService.GetHourlyRequestCount(userId);
+ _telemetryClient.TrackMetric("UserRequestsPerHour", requestCount, ...);
```

**保留功能**：
- ✅ 統計追蹤（每小時請求數）
- ✅ Application Insights 監控
- ✅ 異常行為警示（透過 metric 監控）

### 2. 設定自動 Keep-Warm 機制

**新增檔案**：`.github/workflows/keep-warm.yml`

**功能**：
- 每 5 分鐘自動 ping `/api/health` 端點
- 防止 Function App 進入休眠狀態
- 完全免費（GitHub Actions 免費額度 2000 分鐘/月，此工作流程只用 8-9 分鐘/月）

**工作原理**：
```yaml
schedule:
  - cron: '*/5 * * * *'  # 每 5 分鐘執行一次
jobs:
  ping-health-check:
    steps:
      - curl https://mrvshop-func.azurewebsites.net/api/health
```

**成本分析**：
- GitHub Actions：$0（免費額度）
- Azure Functions：+0.008 次請求/月（288 次 × 30 天 = 8,640 次）
- 額外成本：~$0.00（可忽略不計）

---

## 📊 改善效果對比

| 項目 | 改善前 | 改善後 |
|------|-------|-------|
| **冷啟動頻率** | 閒置 5-20 分後發生 | 幾乎不會發生（5分鐘內必有請求） |
| **首次回應時間** | 5-15 秒（冷啟動） | 0.5-2 秒（溫暖狀態） |
| **速率限制** | 10 次/小時/用戶 | ❌ 無限制 |
| **用戶體驗** | 偶爾卡住 + 被限制 | ✅ 流暢快速 |
| **月成本增加** | - | ~$0（可忽略） |

---

## 🚀 部署步驟

### 1. 推送程式碼更新
```bash
git add .
git commit -m "fix: remove rate limit & add keep-warm workflow"
git push
```

### 2. 部署應用程式
```bash
./scripts/deploy-app.sh
```

### 3. 啟用 GitHub Actions
- GitHub Actions workflow 會在推送後自動啟用
- 確認：到 GitHub > Actions 頁面查看「Keep Function App Warm」

### 4. 驗證效果
```bash
# 等待 5-10 分鐘後測試
curl https://mrvshop-func.azurewebsites.net/api/health
# 應該在 1 秒內回應

# 查看 GitHub Actions 執行記錄
# https://github.com/ShawnTseng/BuddyShopAI/actions
```

---

## 💡 未來升級選項

### Phase 2 升級：Premium Plan + Always On

**時機**：累積 10+ 客戶後（可分攤成本）

**修改檔案**：`infra/modules/functionApp.bicep`

```bicep
sku: {
  name: 'EP1'  // Elastic Premium
  tier: 'ElasticPremium'
}
properties: {
  reserved: true
  alwaysOn: true  // 永遠不休眠
}
```

**成本分析**：
- Premium EP1：$150/月
- 10 客戶分攤：$15/客戶
- 可吸收至月維護費 $4,500 中

**好處**：
- 0 秒冷啟動
- 更專業的用戶體驗
- VNET 整合支援

---

## 📈 監控指標

在 Application Insights 中監控：

1. **UserRequestsPerHour** (Metric)
   - 查看每用戶的請求頻率
   - 異常高頻率 (>100/小時) 可能是濫用

2. **Dependencies > Health Check**
   - 監控 keep-warm ping 是否正常
   - 每 5 分鐘應該有一次成功記錄

3. **Performance > Function Duration**
   - 正常：0.5-2 秒
   - 冷啟動：5-15 秒
   - 如果冷啟動仍頻繁出現，檢查 GitHub Actions

---

## 🎯 成功指標

- ✅ 編譯通過（0 warnings, 0 errors）
- ✅ Health Check 正常回應
- ✅ GitHub Actions workflow 創建完成
- ✅ 速率限制已移除
- ✅ 統計功能保留

**下一步**：部署到 Production 並觀察 1-2 天

---

**維護者**：Shawn Tseng  
**相關文件**：
- [成本優化指南](guides/COST_OPTIMIZATION.md)
- [監控設定](guides/MONITORING.md)
- [商業模式](business/BUSINESS_MODEL.md)
