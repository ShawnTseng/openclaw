# MEMORY.md - Long-Term Memory

> **Last updated:** 2026-03-22 (Repo consolidation — all source code unified under openclaw/02-Repos)
> **Context:** Main Session Only (Protected)
> **Structure:** [P0] Core > [P1] Active > [P2] Context (expires ~30d) > [Graveyard]

---

## [P0] Identity & Core (Permanent)

### Identity
- **Name:** Shawn (Digital Twin) serving **Shawn Tseng (暘暘)**
- **Role:** Strategic Navigator. "Lead me to create maximum life value."
- **Core Values:** Sharp, Efficient, Execution > Explanation.
- **Language:** Chinese (Thinking/Drafts) | English (Docs/Portfolio).

### Life Context
- **Girlfriend (何亭儀)** is going to Australia for university. Applying for **Partner Visa** together.
- **Visa consultant:** Mike. All documents already submitted; awaiting his review.
- **Blocker:** Philippine agent still holding passports → need them back before applying for English-version land ownership certificate (英文版產權證明).
- **Girlfriend's pending docs:** Birth certificate, police check (not Shawn's responsibility).

### Financials
- **Income:** Stable (~120K TWD/month) until **Mar 31**. Part-time from **Apr 1**.
- **Mortgage (寬限期 3 years):** 15,000 TWD/month.
- **Personal Loan (信貸):** 39,200 + 5,500 = **44,700 TWD/month**.
- **Total Fixed Debt:** ~59,700 TWD/month.
- **Property:** Apartment in **中壢, 桃園**.
- **Rental Plan:** Engage 房仲 around **mid-April** to find tenant.

### Workspace Convention
- **All source code lives under:** `openclaw/02-Repos/`
- **Single source of truth:** GitHub (always push before considering local as authoritative)
- **Active dev branch:** `dev` (BuddyShopAI, frontend)

---

## [P1] Active Projects (< 90 days)

### 1. BuddyShopAI
- **Status:** 🟢 **LIVE** (Launched 2026-03-20)
- **Repo:** `openclaw/02-Repos/BuddyShopAI` → `github.com/ShawnTseng/BuddyShopAI` (branch: `dev`)
- **Client:** mrvshop (66MRV / 88MRV fashion e-commerce)
- **Current Phase:** Week 1 post-launch — daily review & adjustment with client
- **Dashboard:** `dashboard/` folder exists in repo (React + Vite + TypeScript, 6 pages already built)
  - Pages: ConversationsPage, PendingActionsPage, ConfigPage, AdminsPage, SystemPage, LoginPage
  - Status: partially built — assess before adding more
- **Tech Stack:** .NET 8 Azure Functions + LINE Messaging API + Azure OpenAI + Semantic Kernel
- **Key Architecture:** Prompt Engineering (`[範本]` tags + `temperature=0`), `[SPLIT]` multi-bubble, auto-handoff (7 keywords)
- **Revenue:** Awaiting deposit/consultancy fee
- **Next Phase Plan:** `openclaw/03-Projects/BuddyShopAI/Next-Phase-Strategy.md`

### 2. Career Repositioning
- **Pivot (4AM Breakthrough 2026-02-17):**
  - **To:** Senior Cloud Platform Engineer & .NET Architect (DevOps Focus)
- **Tagline:** "Bridging Development and Operations with Cloud-Native .NET and AI"
- **Pending:**
  - [ ] Update GitHub Profile README
  - [ ] Update personal website (shawn-tseng.vercel.app)
  - [ ] Update LinkedIn profile

### 3. AZ-305 Certification
- **Start Date:** 2026-04-01
- ✅ AZ-104 certified; ✅ Japan trip completed (2026-02-23 ~ 03-05)

### 4. OpenClaw System
- **Status:** ⏸ **Paused** — GitHub Copilot nearly maxed out (1,500 req/month)
- **Resume plan:** Reassess after Apr 1
- **Portfolio repo:** `openclaw/02-Repos/openclaw` → `github.com/ShawnTseng/openclaw`

---

## [P2] Near-term Context (expires ~2026-04-22)

### Partner Visa (Girlfriend's AU University)
- Consultant: **Mike** — docs submitted, awaiting response
- Blocker: Passports held by Philippine agent
- Reference: `openclaw/01-Daily/Partner-Visa-Tracker.md`

### Property Rental (桃園中壢)
- Mid-April: Engage 房仲 to find tenant

### BuddyShopAI Dashboard
- Dashboard folder already exists — assess current state before building more

---

## [Archive] Patterns & Lessons

### Career
- AI commoditizes coding → Value moves to infrastructure/operations
- Simpler solutions (prompt engineering) beat complex architectures

### Technical
- .NET + Azure First: Aligns with AU enterprise market
- IaC non-negotiable: Bicep/Terraform = Platform Engineering credibility
- Markdown is truth: Chat is ephemeral; decisions must live in files
- Issue-First: Never code without a written spec

---

## [Graveyard]

| Project | Note | Removed |
|---------|------|---------|
| SmartCommerceAI | BuddyShopAI 的前身原型，無 git remote，已刪除 | 2026-03-22 |
| SeafoodAI / Ian Startup | 同一個案子（呂以恆 via Nathan）。技術會議沒開成 | 2026-03-22 |
| fluffyflint | 女友何亭儀個人網站，未持續 follow-up。Repo 保留於 `02-Repos/fluffyflint` | 2026-03-22 |
| frontend (Cardano DApp) | sovx-dev org，Cardano governance DApp，暫停中。Repo 保留於 `02-Repos/frontend` | 2026-03-22 |
| LocalRAG | 與 BuddyShopAI 重疊，暫停。Repo 保留於 `02-Repos/LocalRAG` | 2026-03-22 |
| OpenClaw Cron Jobs | 系統暫停 | 2026-03-22 |

---

## Critical File Locations

| 項目 | 路徑 |
|------|------|
| BuddyShopAI Repo | `openclaw/02-Repos/BuddyShopAI` (dev branch) |
| BuddyShopAI Next Phase | `openclaw/03-Projects/BuddyShopAI/Next-Phase-Strategy.md` |
| Resume | `openclaw/02-Repos/Content-Matrix/Drafts/ShawnTseng-Resume-2026-Architect.md` |
| Partner Visa Tracker | `openclaw/01-Daily/Partner-Visa-Tracker.md` |
| Content Strategy | `openclaw/02-Repos/Content-Matrix/Strategy.md` |
| Personal Website | https://github.com/ShawnTseng/shawn-tseng |
| Portfolio | https://github.com/ShawnTseng/openclaw |
