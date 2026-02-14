# OpenClaw SaaS - 成本驗證待辦

> **Created:** 2026-02-14 08:41  
> **Deadline:** 1 週內完成  
> **Owner:** 暘暘

---

## 🎯 Phase 1: 成本驗證待辦事項

### 1️⃣ VPS 成本調查

**目標:** 確認 4C/8G VPS 實際價格

**待辦:**
- [ ] **Linode (Akamai):** 查詢 4C/8G 方案月費 (台幣)
- [ ] **DigitalOcean:** 查詢 4C/8G Droplet 月費 (台幣)
- [ ] **Vultr:** 查詢 4C/8G 方案月費 (台幣)
- [ ] **Hetzner:** 查詢 4C/8G 方案月費 (台幣) - 歐洲便宜選項
- [ ] **AWS Lightsail:** 查詢 4C/8G 月費 (台幣)

**需確認:**
- 月費價格 (轉換成台幣)
- 流量限制
- 是否支援 Docker
- 網路速度 (台灣/亞洲連線)

**成功標準:** 找到 **NT$1,500/月 或更低** 的方案

---

### 2️⃣ Anthropic API 計費驗證

**目標:** 確認 Claude Sonnet 4.5 實際計費方式

**待辦:**
- [ ] 訪問 Anthropic 官網 pricing 頁面
- [ ] 確認 Claude Sonnet 4.5 存在 (或最新 Sonnet 版本)
- [ ] 確認 Input/Output token 價格 (USD)
- [ ] 轉換成台幣 (匯率 1 USD = NT$30)
- [ ] 確認是否有最低消費或月費
- [ ] 確認 API quota 限制

**參考資料:**
- 官網: https://www.anthropic.com/pricing
- Docs: https://docs.anthropic.com/

**成功標準:** 
- Input ≤ NT$9/100萬 tokens
- Output ≤ NT$45/100萬 tokens

---

### 3️⃣ Docker 容量測試

**目標:** 確認單台 4C/8G VPS 可容納多少 OpenClaw containers

**待辦:**
- [ ] 在本機測試 Docker 資源限制
  ```bash
  docker run --cpus=0.5 --memory=512m openclaw/openclaw:latest
  ```
- [ ] 計算理論容量:
  - 4 Core ÷ 0.5 CPU/container = 8 containers (理論)
  - 8GB RAM ÷ 512MB/container = 16 containers (理論)
  - **保守估計: 6-8 個 containers**
- [ ] 考慮系統開銷 (Nginx, 監控, 等)
- [ ] 確認網路頻寬是否成為瓶頸

**成功標準:** 確認 **10-15 個 containers** 可行

---

### 4️⃣ 真實成本計算

**目標:** 根據驗證結果計算實際成本

**待辦:**
- [ ] 填入真實 VPS 價格
- [ ] 填入真實 Anthropic API 價格
- [ ] 計算每人分攤成本
- [ ] 計算安全邊際 (定價 ÷ 成本)
- [ ] 確認毛利率 >85%

**試算表格:**
```
項目                  | 成本 (NT$/月)
--------------------|-------------
VPS (實際價格)        | ???
Anthropic API (中度)  | ???
總成本 (每人)         | ???
定價 (輕量方案)       | 1,800
安全邊際             | ???x
毛利率               | ???%
```

**成功標準:** 安全邊際 **>10x**

---

## 📅 執行時程

| 任務 | 預估時間 | 建議執行時間 |
|------|----------|--------------|
| VPS 成本調查 | 30 分鐘 | 隨時可做 |
| Anthropic API 驗證 | 20 分鐘 | 隨時可做 |
| Docker 容量測試 | 1 小時 | 本機測試 |
| 成本計算 | 30 分鐘 | 完成前三項後 |

**總計:** 約 2-3 小時

---

## ✅ 完成後決策

**如果成本驗證通過 (安全邊際 >10x):**
- ✅ 進入 Phase 2: PoC 開發
- 時機: 宿霧期間 (4/12-5/12) 可嘗試

**如果成本過高 (安全邊際 <10x):**
- ❌ 放棄此商業模式
- 或調整定價/quota

**如果技術不可行 (Docker 容量不足):**
- 🔄 改用更大 VPS (成本重算)
- 或減少每台容納人數

---

## 📝 備註

- 這些待辦可在 **任何零碎時間** 完成
- **不需要寫 Code**，純粹調查 + 計算
- 建議在 **春節假期** 或 **日本旅行前** 完成
- 完成後更新 `OpenClaw-SaaS-Business-Plan.md`

---

**Next:** 開始執行第一項 (VPS 成本調查)
