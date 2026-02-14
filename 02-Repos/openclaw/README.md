# OpenClaw Workspace (Meta-Repo)

> **Repository:** [github.com/ShawnTseng/openclaw](https://github.com/ShawnTseng/openclaw)  
> **Purpose:** Shawn Tseng's AI-powered digital workspace — doubling as a technical portfolio for senior engineering roles.

---

## 🏗️ Architecture

This is a **Meta-Repo**: a single workspace that organizes multiple projects, life operations, and knowledge systems under one roof.

```
~/.openclaw/workspace/
├── 00-Index/              # Entry points & navigation
│   └── Knowledge-Portal.md
├── 01-Daily/              # Auto-generated daily logs (English)
├── 02-Repos/              # Code projects & technical assets
│   ├── BuddyShopAI/      # AI-powered e-commerce (SaaS Pivot)
│   ├── LocalRAG/          # Enterprise RAG solution
│   ├── Content-Matrix/    # Automated content factory
│   ├── AI-Debate-Arena/   # Multi-agent debate system
│   └── openclaw/          # This project's docs & strategy
├── 03-Life-Ops/           # Non-code life projects
│   ├── Australia-Migration/
│   ├── Financial-Independence/
│   └── Travel/
├── 04-Knowledge-Base/     # Reusable patterns & references
│   ├── AI-Patterns/
│   └── DevOps/
├── memory/                # Long-term AI memory (private)
├── private/               # Sensitive data (private)
└── scripts/               # Automation scripts
```

## 🤖 Automation (Cron Jobs)

All automation runs via **OpenClaw's built-in cron scheduler** (not macOS crontab).

| Job | Schedule | Purpose |
|-----|----------|---------|
| Morning TODO | 07:00 daily | Priority-sorted task list |
| Daily Diary | 22:00 daily | Auto-generate daily log from conversations |
| Evening Preview | 22:30 daily | Tomorrow's priorities + bedtime reminder |
| Daily Git Sync | 23:55 daily | Auto commit & push to GitHub |
| Memory Consolidation | 03:00 daily | Refine long-term memory |
| Weekly Report | Sun 21:00 | Weekly summary from daily logs |
| System Maintenance | Sun 10:00 | Security audit + update checks |

## 🔐 Security Model

- **Public:** `00-Index/`, `01-Daily/`, `02-Repos/`, `04-Knowledge-Base/`
- **Private (gitignored):** `memory/`, `private/`, `USER.md`, `MEMORY.md`, `.env`
- **Strategy:** History was force-pushed to sanitize leaked secrets.

## 🛠️ Tech Stack

- **Runtime:** OpenClaw v2026.2.13 on Mac Mini (Darwin arm64)
- **Primary Model:** Google Antigravity (Gemini 3 Pro High)
- **Fallback Model:** GitHub Copilot (Claude Opus 4.6)
- **Channel:** Discord
- **VCS:** Git → GitHub (daily auto-sync)

## 📈 Roadmap

- [x] Workspace restructure (Meta-Repo)
- [x] Full English conversion
- [x] Cron automation (7 jobs)
- [ ] Content Matrix: First blog post
- [ ] BuddyShopAI: mrvshop launch
- [ ] LocalRAG: First enterprise pilot
