# 💰 成本優化策略

> 如何將每租戶成本控制在 $10 USD/月以內（含無冷啟動保證）

---

## 當前成本結構

| 服務 | 月成本 | 優化策略 |
|------|--------|---------|
| Azure OpenAI | $2-3 | 訊息防抖、使用量追蹤 |
| Functions (Flex Prod) | ~$5 | Always Ready 1 instance，無冷啟動 |
| Functions (Staging) | $0 | Consumption Plan 免費額度 |
| Storage | $0.01 | 僅儲存對話歷史 |
| Key Vault | $0.03 | 最少 Secrets |
| App Insights | $0 | 免費 5GB/月 |
| **總計** | **~$7.50** | |

---

## 已實施的優化

### 1. 訊息防抖（3秒合併）

減少 40-60% AI 呼叫：

```
用戶連續傳送:
"你們"
"有沒有"
"黑色外套"

→ 合併為一次 AI 呼叫: "你們有沒有黑色外套"
```

### 2. 對話歷史上限（10則）

控制每次 token 消耗：

```
Context = System Prompt + 最近10則對話 + 新訊息
約 500-1000 tokens per request
```

### 3. 使用量追蹤（Application Insights）

透過 `UserRequestsPerHour` 自訂指標監控用量，不限制付費客戶使用（v1.1.0 已移除硬性速率限制）。

### 4. Flex Consumption Plan + Always Ready

Production 使用 Flex Consumption Plan (FC1)，Always Ready 1 instance 避免冷啟動。
成本僅 ~$5/月，遠低於 Premium Plan (~$158/月)。
Staging 維持 Consumption Plan，完全按用量付費。

---

## 進一步優化建議

### P1: Prompt 精簡

- 縮短 System Prompt
- 使用更具體的指令
- 預估節省 10-20% token

### P2: Cache FAQ 回答

- 常見問題直接返回預存答案
- 不呼叫 OpenAI
- 預估節省 30-40% 成本

### P3: 使用 Batch API（未來）

- Azure OpenAI Batch API 成本減半
- 適合非即時場景

---

## 成本告警設定（規劃中）

```bash
# 設定預算告警
az consumption budget create \
  --amount 5 \
  --budget-name ${TENANT_ID}-budget \
  --resource-group rg-${TENANT_ID}-prod \
  --time-grain Monthly
```

---

**目標**: 每租戶 < $10 USD/月（含無冷啟動保證）  
**當前**: ~$7.50 USD/月 ✅
