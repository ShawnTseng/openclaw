# 💼 Buddy ShopAI 商業模式

> 最後更新：2026-02-13  
> 狀態：Phase 1 已上線（多租戶架構已實作）

---

## 1. 核心定位 (Value Proposition)

- **產品定義**：服飾電商專用 AI 智能店長 (Fashion Vertical SaaS) — **Buddy ShopAI**
- **目標市場**：月營收 50 萬 ~ 500 萬的台灣中小型服飾選品店
- **解決痛點**：
  - 尺寸與材質諮詢：AI 判斷身高體重與材質特性
  - 退貨防禦：AI 預先過濾不適合的尺寸，降低逆物流成本
  - 活動即時同步：低門檻管理體驗（目前用 JSON，未來規劃 Google Sheet CMS）

### 競爭優勢 (Moat)

- **技術自主**：Azure Serverless + gpt-4o-mini，高度客製化彈性
- **成本結構**：「買斷建置 + 低維護費」，長期持有成本低於 Omnichat 等平台

---

## 2. 目標客群 (Target Customer)

| 條件 | 說明 |
|------|------|
| 產業 | 服飾電商（服裝、配件、鞋包） |
| 規模 | 月營收 50 萬 - 500 萬的中小型店家 |
| 平台 | 已有 LINE 官方帳號（或願意申請） |
| 痛點 | 客服回覆量大、FAQ 重複、夜間無人值班 |
| 預算 | 能接受月費 $4,500 起的 SaaS 服務 |

---

## 3. 產品發展路徑 (Product Roadmap)

### Phase 1：智能客服 (Smart Clerk) — ✅ 已上線

- 功能：基礎問答、運費查詢、FAQ 自動回覆、尺寸建議
- 技術：Azure Functions + Azure OpenAI + configs/{tenantId}.json
- 價值：節省真人客服 60-70% 時間

### Phase 2：視覺辨識與進階知識庫 (Visual Stylist) — 📋 規劃中

- 以圖搜圖（GPT-4o Vision）
- No-Code CMS（Google Sheets 取代靜態 JSON）
- PDF 知識庫 (RAG)

### Phase 3：系統深度串接 (System Integrator) — 📋 規劃中

- 串接 Shopline / Cyberbiz / POS API
- 全自動化服務

---

## 4. 服務邊界

- **包含**：AI 客服部署、Prompt 調教、LINE 串接、商店設定、系統監控
- **除外**：Phase 2/3 功能、行銷素材製作、主動推播設定

---

## 5. 現金流里程碑

| 里程碑 | 目標 | 預估收入 |
|-------|------|---------|
| **A：市場驗證** | 簽下 3 家客戶 | 建置 $108K + 月維護 $13.5K |
| **B：規模化** | 累積 10 家客戶 | 累積建置 $360K + 月維護 $45K |
| **C：功能升級** | Phase 2 Upsell | 升級費 $75K (50% 轉化率) |

---

**相關文檔**：
- [定價策略](PRICING.md) — 收費結構與付款方式
- [客戶上線流程](ONBOARDING.md) — 新客戶導入 SOP

---

**維護者**：Shawn Tseng  
**License**：MIT
