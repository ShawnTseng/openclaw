# AGENTS.md - The Workspace Manual

> **Context:** This is your home directory. Treat it with respect.

## 🚀 Boot Sequence

1. **Read `SOUL.md`:** Re-align with core persona and mission.
2. **Read `USER.md`:** Understand Shawn's context, goals, preferences, and constraints.
3. **Read `memory/MEMORY.md`:** Access long-term strategic context.
4. **Read today's daily note at `daily/[today's date].md`** if it exists.

## 🗂️ Directory Structure

```
openclaw/
├── SOUL.md / USER.md / AGENTS.md / IDENTITY.md / TOOLS.md / HEARTBEAT.md
├── memory/MEMORY.md      ← Long-term curated context
├── daily/                ← Daily logs (YYYY-MM-DD.md)
├── content/              ← Articles, resume, content strategy
├── projects/             ← Project strategy docs (not source code)
├── life/                 ← Visa, travel, finance, identity
├── knowledge/            ← Technical notes (future use)
└── repos/                ← Source code repos (each independent git, fully gitignored)
    ├── BuddyShopAI/
    ├── LocalRAG/
    ├── fluffyflint/
    └── frontend/
```

## 🧠 Memory Protocol

- **Daily Logs:** `daily/YYYY-MM-DD.md` — raw events, thoughts, drafts
- **Long-Term:** `memory/MEMORY.md` — curated insights, decisions, facts
- **Rule:** Never rely on mental notes. Markdown is truth; chat is ephemeral.

## ⚡ Execution Protocol

- **Issue-First:** Before writing code, define the task in a Markdown spec.
- **Markdown is King:** All knowledge, decisions, specs must live in Markdown.
- **Verify before reporting:** Check git log before claiming something was done.

## 🛡️ Security & Privacy

- **Private zones:** `memory/`, `private/`, `.env` — never commit secrets
- **Public zones:** `content/`, `knowledge/` — portfolio-safe
- **Always check `.gitignore` before git operations**
- **`repos/` is fully gitignored** — each repo manages its own remote
