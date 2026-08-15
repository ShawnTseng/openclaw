# OpenClaw — Shawn's Digital Twin Workspace

Personal AI workspace and knowledge base for Shawn Tseng（曾季暘）。

## Structure

```
openclaw/                     (this repo — public config)
├── SOUL.md          — AI persona and principles
├── USER.md          — Shawn's context, preferences, validation rules
├── AGENTS.md        — Directory structure, boot sequence, memory protocol
├── CLAUDE.md        — Claude Desktop-specific supplement to AGENTS.md
├── TOOLS.md         — Local machine config (model version, credentials, editor prefs)
├── HEARTBEAT.md     — Proactive checks to run at session start
├── content/         — Articles, resume, content strategy
├── projects/        — Project strategy docs
├── life/            — Visa, travel, finance
├── knowledge/       — Technical notes
└── repos/           — Source code (gitignored, each has own remote)
    ├── BuddyShopAI  → github.com/ShawnTseng/BuddyShopAI
    ├── LocalRAG     → github.com/ShawnTseng/LocalRAG
    ├── fluffyflint  → github.com/ShawnTseng/fluffyflint
    └── frontend     → github.com/sovx-dev/frontend

openclaw-private/              (separate git repo — private)
├── memory/
│   ├── MEMORY.md         — hot-path long-term memory, read every boot
│   ├── LIFE-ROADMAP.md   — 30yr strategy/identity deep-dives, read on demand
│   ├── cron-reference.md, honami.md
│   └── archive/          — completed one-off tasks
├── daily/           — Daily logs (YYYY-MM-DD.md)
└── life/, projects/
```

## Active Projects

- **BuddyShopAI** — LINE AI customer service, live since 2026-03-20
- **AZ-305** — Starting 2026-04-01（⚠️ 沒出現在 MEMORY.md 的證照清單裡，狀態待確認——完成了、放棄了，還是被 AZ-400 取代了？）

## Tech Stack

Claude Desktop (primary AI) · .NET 8 · Azure · React · TypeScript · Bicep IaC
