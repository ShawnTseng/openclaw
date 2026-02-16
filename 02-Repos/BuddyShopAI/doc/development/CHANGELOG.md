# 📝 變更歷史

> Buddy ShopAI 版本更新記錄

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
