# AGENTS.md - The Workspace Manual

> **Context:** This is your home directory. Treat it with respect.

## 🚀 Boot Sequence

1. **Read `SOUL.md`:** Re-align with core persona and mission.
2. **Read `USER.md`:** Understand Shawn's context, goals, preferences, and constraints.
3. **Read `memory/MEMORY.md`:** Access long-term strategic context (hot-path only — `LIFE-ROADMAP.md` and `archive/` are read on demand, not every boot).
4. **Read today's daily note at `daily/[today's date].md`** if it exists.
5. If any file is missing, skip it and continue — don't block the boot sequence on a missing file.

## 🗂️ Directory Structure

```
openclaw/                          (public config — this repo)
├── SOUL.md / USER.md / AGENTS.md / CLAUDE.md / TOOLS.md / HEARTBEAT.md / README.md
├── content/              ← Articles, resume, content strategy
├── projects/             ← Project strategy docs (not source code)
├── life/                 ← Visa, travel, finance, identity
├── knowledge/            ← Technical notes (future use)
└── repos/                ← Source code repos (each independent git, fully gitignored)
    ├── BuddyShopAI/
    ├── LocalRAG/
    ├── fluffyflint/
    └── frontend/

openclaw-private/                  (private, separate git repo)
├── memory/
│   ├── MEMORY.md         ← hot-path curated context, read every boot
│   ├── LIFE-ROADMAP.md   ← 30yr strategy/identity deep-dives, read on demand
│   └── archive/          ← completed one-off tasks (e.g. relocation log)
├── daily/                ← Daily logs (YYYY-MM-DD.md)
└── life/, projects/
```

> **2026-08 整理備註：** IDENTITY.md 已移除（內容跟 SOUL.md + TOOLS.md 重複，含過時的 model 版本），git 歷史仍可找回。CLAUDE.md 精簡為 AGENTS.md 的 Claude Desktop 專屬補充，不再重複整份 boot sequence。

## 🧠 Memory Protocol

- **Daily Logs:** `daily/YYYY-MM-DD.md` — raw events, thoughts, drafts
- **Long-Term (hot path):** `memory/MEMORY.md` — curated insights, decisions, facts that matter *every* session
- **Long-Term (on demand):** `memory/LIFE-ROADMAP.md` — 30yr strategy, travel, academic, identity content; pull it in when the conversation actually goes there
- **Archive:** `memory/archive/` — completed, dated one-off tasks kept for reference, not re-read by default
- **Rule:** Never rely on mental notes. Markdown is truth; chat is ephemeral. Keep MEMORY.md lean — if a section is done or expired, move it, don't leave it to rot.

## ⚡ Execution Protocol

- **Issue-First:** Before writing code, define the task in a Markdown spec.
- **Markdown is King:** All knowledge, decisions, specs must live in Markdown.
- **Verify before reporting:** Check git log before claiming something was done.

## 🛡️ Security & Privacy

- **Private zones:** `memory/`, `private/`, `.env` — never commit secrets
- **Public zones:** `content/`, `knowledge/` — portfolio-safe
- **Always check `.gitignore` before git operations**
- **`repos/` is fully gitignored** — each repo manages its own remote
