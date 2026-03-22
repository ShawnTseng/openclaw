# MEMORY.md - Long-Term Memory

> **Last updated:** 2026-03-22 (Full audit — all files scanned, all sources verified)
> **Structure:** [P0] Core > [P1] Active > [P2] Context (expires ~30d) > [Graveyard]

---

## [P0] Identity & Core (Permanent)

### Identity
- **Name:** Shawn (Digital Twin) serving **Shawn Tseng（曾季暘 / 暘暘）**
- **Role:** Strategic Navigator. "Lead me to create maximum life value."
- **Core Values:** Sharp, Efficient, Execution > Explanation.
- **Language:** Chinese (Thinking/Drafts) | English (Docs/Portfolio).
- **Tools:** Claude Desktop (primary AI, replacing Google Antigravity) + VS Code + GitHub Copilot（Copilot 額度快用完）

### Personal
- **Address:** 14F, No. 81, Sec. 1, Qingfeng Rd., Zhongli Dist., Taoyuan City
- **Health:** Stage 2 Chronic Kidney Disease (stable, daily medication)
- **Morning routine:** 7:30 AM run at Calligraphy Park

### Relationship — 何亭儀（Ting）
- **Together since:** 2022-09-01（McDonald's confession）
- **Met:** 2022-07-15 at Soka Gakkai Taipei Jinzhou Center
- **Living together since:** 2023-07-01（3 年同居，滿足 de facto 12 個月要求）
- **IELTS:** Both achieved 6.5 in December 2025
- **Plan:** Ting 去澳洲讀研究所 → Shawn 以伴侶身分附簽跟去

### Visa — Student Visa Subclass 500, Secondary Applicant
- **類型：** Student Visa 500 Secondary Applicant（伴侶附簽於 Ting 的學生簽）
- **不是：** Partner Visa 820/801（舊記憶錯誤，已修正）
- **Primary:** Ting（學生）；**Secondary:** Shawn（伴侶）
- **Work rights:** 無限制工時（Ting 讀研究所，伴侶不受 48hr/fortnight 限制）
- **簽證期限：** 同 Ting 學生簽
- **Consultant:** Mike — 文件已全部交給他，等待回覆
- **Blocker:** 護照卡在菲律賓代辦，等拿回後申請英文版產權證明
- **Shawn 聲明書：** `openclaw/03-Life-Ops/Australia-Migration/Visa-Statement-Shawn.md` ✅
- **完整 checklist：** `openclaw/03-Life-Ops/Australia-Migration/Partner-Visa-Checklist-Complete.md`（注意：內容標題寫 820/801，但實際是附簽申請，部分表格要求不適用，待與 Mike 確認）

### Financials
- **Income:** ~120K TWD/month until **2026-03-31**，**2026-04-01 起轉兼職**
- **Mortgage（寬限期 3 年）:** 15,000 TWD/month
- **Personal Loan（信貸）:** 39,200 + 5,500 = **44,700 TWD/month**
- **Total Fixed Debt:** ~59,700 TWD/month
- **Property:** 2025-10 購入，中壢, 桃園
- **Rental Plan:** 四月中找房仲出租

### Workspace Convention
- **Source code:** `openclaw/02-Repos/<repo>/`，各自獨立 git repo
- **openclaw git:** 只追蹤知識庫，不 track 02-Repos 內容（已修正並 push）
- **Dev branch:** `dev`（BuddyShopAI, frontend）
- **MCP tools:** obsidian-vault（讀寫檔案）+ shell（執行 git）

---

## [P1] Active Projects (< 90 days)

### 1. BuddyShopAI
- **Status:** 🟢 **LIVE**（2026-03-20 上線）
- **Repo:** `openclaw/02-Repos/BuddyShopAI` → `github.com/ShawnTseng/BuddyShopAI`（branch: `dev`）
- **Client:** mrvshop（66MRV / 88MRV，fashion e-commerce）
- **Current Phase:** Week 1 post-launch — 每日與客戶 review 調整
- **Revenue:** 等待 deposit / 顧問費（2026-03-15 台中客戶會議後確認）
- **Tech Stack:** .NET 8 Azure Functions + LINE Messaging API + Azure OpenAI + Semantic Kernel
- **Architecture:** Prompt Engineering（`[範本]` tags + `temperature=0`）、`[SPLIT]` multi-bubble、auto-handoff（7 keywords）
- **Dashboard:** `dashboard/` 已存在 repo（React + Vite + TypeScript，6 pages 已建）
  - Pages: ConversationsPage, PendingActionsPage, ConfigPage, AdminsPage, SystemPage, LoginPage
  - 繼續開發前先 assess 現有狀態
- **Next Phase Plan:** `openclaw/03-Projects/BuddyShopAI/Next-Phase-Strategy.md`
- **Key Lesson:** Prompt Engineering > complex code logic（4.5h 完成 29 commits 的功能）

### 2. Career Repositioning
- **Pivot（4AM Breakthrough, 2026-02-17）:**
  - **To:** Senior Cloud Platform Engineer & .NET Architect（DevOps Focus）
- **Tagline:** "Bridging Development and Operations with Cloud-Native .NET and AI"
- **Key Skills:** IaC (Bicep), CI/CD (Azure DevOps/GitHub Actions), Kubernetes/Container Apps, SRE
- **Pending:**
  - [ ] Update GitHub Profile README
  - [ ] Update personal website (shawn-tseng.vercel.app)
  - [ ] Update LinkedIn profile
- **Resume:** `openclaw/02-Repos/Content-Matrix/Drafts/ShawnTseng-Resume-2026-Architect.md`

### 3. AZ-305 Certification
- **Start:** 2026-04-01
- ✅ AZ-104 certified；✅ Japan trip completed (2026-02-23 ~ 03-05)
- **Strategy:** AZ-305 first（leverage AZ-104 overlap）→ 用 BuddyShopAI 展示 AZ-400 技能

### 4. OpenClaw System
- **Status:** ⏸ 暫停 — GitHub Copilot 額度快用完（1,500 req/month）
- **Resume plan:** 2026-04-01 轉兼職後重新評估
- **Portfolio:** `github.com/ShawnTseng/openclaw`

---

## [P2] Near-term Context (expires ~2026-04-22)

### Visa 緊急事項
- 護照待菲律賓代辦歸還 → 拿回後申請英文版產權證明 → 交給 Mike
- 菲律賓語言學校（Apr–May 2026）：同時是英文準備 + 簽證 social evidence

### Property Rental（桃園中壢）
- 四月中：找房仲出租

### BuddyShopAI Week 1
- 每日 client review
- Dashboard：先 assess 現有 code 再繼續

---

## [Archive] Patterns & Lessons

### Career
- AI 商品化純 coding → 價值往 infrastructure/operations 移動
- AU 企業市場：Azure > AWS，DevOps/SRE > pure dev
- 澳洲研究所伴侶簽工作權無限制 → 可以全力工作

### Technical
- Prompt Engineering 勝過複雜架構（BuddyShopAI 實戰驗證）
- .NET 作為護城河：企業穩定性 + 現代雲端能力
- IaC 不可或缺：Bicep/Terraform = Platform Engineering 公信力
- Markdown is truth：chat 是臨時的，決策必須在檔案裡
- Issue-First：沒有 spec 就不寫 code

### Business
- Sell results, not tools：客戶買的是 ROI
- Location-independent 模式驗證：11 天日本旅行，zero incidents

---

## [Graveyard]

| Project | Note | Removed |
|---------|------|---------|
| SeafoodAI / Ian Startup | 同一個案子（呂以恆 via Nathan）。3/12 & 3/19 兩次都沒開成 | 2026-03-22 |
| SmartCommerceAI | BuddyShopAI 前身原型，無 git remote，已刪除 | 2026-03-22 |
| BuddyShopAI → Real Estate AI SDR Pivot | 2026-02-14 的策略想法，沒有執行，繼續服務 fashion e-commerce | 2026-03-22 |
| fluffyflint | Ting 個人網站，未持續 follow-up。Repo 保留 | 2026-03-22 |
| frontend (Cardano DApp) | sovx-dev org，暫停。Repo 保留 | 2026-03-22 |
| LocalRAG | 與 BuddyShopAI 重疊，暫停。Repo 保留 | 2026-03-22 |
| OpenClaw Cron Jobs | 系統暫停 | 2026-03-22 |
| OpenClaw SaaS Plan | 早期探索（2026-02-14），低優先，未驗證 | 2026-03-22 |
| Google Antigravity (Gemini) | 已被 Claude Desktop 取代為主力 AI | 2026-03-22 |

---

## Critical File Locations

| 項目 | 路徑 |
|------|------|
| BuddyShopAI Repo | `openclaw/02-Repos/BuddyShopAI` (dev) |
| BuddyShopAI Next Phase | `openclaw/03-Projects/BuddyShopAI/Next-Phase-Strategy.md` |
| Resume | `openclaw/02-Repos/Content-Matrix/Drafts/ShawnTseng-Resume-2026-Architect.md` |
| Visa Statement (Shawn) | `openclaw/03-Life-Ops/Australia-Migration/Visa-Statement-Shawn.md` |
| Visa Checklist | `openclaw/03-Life-Ops/Australia-Migration/Partner-Visa-Checklist-Complete.md` |
| Visa Tracker | `openclaw/01-Daily/Partner-Visa-Tracker.md` |
| Japan Trip (Evidence) | `openclaw/03-Life-Ops/Travel/Japan-Trip-March-2026.md` |
| Content Strategy | `openclaw/02-Repos/Content-Matrix/Strategy.md` |
| Personal Website | https://github.com/ShawnTseng/shawn-tseng |
| Portfolio | https://github.com/ShawnTseng/openclaw |
