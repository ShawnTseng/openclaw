## 🔔 Cron 通知優化 🆕

### 調整背景
- 早晨 3 個通知（06:50/53/56）太密集，感覺很吵
- AI 新聞每日看沒意義（除非重大事件）
- 加密貨幣採週線交易策略，每日行情是雜音

### 執行調整
**停用每日通知：**
- ❌ Morning Briefing Part 2 (AI Signal) - 每日 06:53
- ❌ Morning Briefing Part 3 (Crypto Pulse) - 每日 06:56

**新增週報（週日）：**
- ✅ Weekly AI Highlights - 週日 08:00
  - 僅報告重大事件（GPT-5 級別發布、API 價格>50% 變動、開源模型突破）
  - 無重大事件 = "No significant news this week"
- ✅ Weekly Crypto Market Analysis - 週日 09:00
  - 週線級別分析（200WMA、ETF Flows、Wyckoff）
  - 僅報告明確訊號（熊底確認、突破、頂部訊號）
  - 無明確訊號 = "Hold / Continue observation"

**優化每日通知：**
- ✅ Morning Brief - Daily Priorities - 改為 07:00 整點
  - 聚焦 3 個今日優先事項（Partner Visa、BuddyShopAI、Health）
  - 簡潔版，去除雜訊

### 調整後的通知時間表

**每日 (4 個)：**
- 07:00 - Morning Brief (Daily Priorities)
- 21:30 - Daily Diary Generation
- 22:30 - Evening TODO Preview
- 23:00 - Daily Git Sync
- 03:00 - Memory Consolidation (背景執行)

**週報 (5 個)：**
- 週六 20:00 - Weekly Content Refinement
- 週日 08:00 - 🆕 Weekly AI Highlights (重大事件)
- 週日 09:00 - 🆕 Weekly Crypto Analysis (週線訊號)
- 週日 10:00 - Weekly System Maintenance
- 週日 22:00 - Weekly Self-Reflection

### 效益
- ✅ 早晨通知從 3 個減為 1 個（安靜 66%）
- ✅ AI/Crypto 改為週線級別（符合實際需求）
- ✅ 保留重要自動化（Diary、Git Sync、Memory）
- ✅ 重大事件仍能即時掌握（週報篩選）

---
