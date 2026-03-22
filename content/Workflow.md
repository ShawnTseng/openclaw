# Content Matrix Workflow

> **Purpose:** Transform daily OpenClaw conversations into monetized blog content.

---

## 🔄 The Pipeline

```
You (Daily Chat)
    ↓
[21:30] Daily Diary → 01-Daily/YYYY-MM-DD.md
    ↓
[22:30] Evening Review → Discord notification + GitHub link
    ↓
[23:00] Git Sync → Push to GitHub
    ↓
[Sat 20:00] Weekly Content Refinement → Agent reads week's diaries
    ↓
Drafts/ ← Agent writes blog draft here
    ↓
You review & approve
    ↓
Published/ ← Final version moved here
    ↓
Post to Medium + Hugo
```

---

## 📁 Folder Structure

```
02-Repos/Content-Matrix/
├── Strategy.md          # Platform strategy & monetization plan
├── Backlog.md           # Ideas queue (topic pool)
├── Workflow.md          # This file (you are here)
├── Drafts/              # Work-in-progress articles (AI-generated, human-reviewed)
│   └── 001-digital-twin-openclaw.md
└── Published/           # Approved & posted articles (final versions)
```

### Why two folders?

- **`Drafts/`** = Kitchen (raw ingredients, experiments, half-baked ideas)
- **`Published/`** = Restaurant (polished, approved, served to the public)

You never publish from `Drafts/` directly. The review step is mandatory.

---

## 📋 Weekly Routine

### Saturday (Content Day)
1. **20:00** — Cron job auto-generates a draft from the week's diaries
2. **20:30** — You receive a Discord notification with the draft
3. **Anytime** — You review, edit, or reject

### When Ready to Publish
1. Tell me: "Publish 001"
2. I will:
   - Polish the draft (grammar, formatting, SEO title)
   - Move from `Drafts/` → `Published/`
   - Post to Medium (via API or manual copy)
   - Cross-post summary to LinkedIn
   - Update `Backlog.md` status

---

## 🎯 Content Guidelines

- **Language:** English only
- **Length:** 1500-2500 words (7-10 min read)
- **Tone:** Conversational + Technical (show real code/configs)
- **Structure:** Hook → Problem → Solution → Result → CTA
- **CTA:** Always end with GitHub link + "hire me" option

---

## 📊 Tracking

| # | Title | Status | Platform | Date |
|---|-------|--------|----------|------|
| 001 | How I Built a Digital Twin with OpenClaw | Draft | — | — |
