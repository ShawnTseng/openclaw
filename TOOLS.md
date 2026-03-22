# TOOLS.md - Local Configuration Notes

> **Context:** This file stores environment-specific settings for the local OpenClaw instance.
> **Last updated:** 2026-03-22

## 🏗️ Infrastructure

- **Host:** Shawn's Mac Mini
- **Network:** Home Network (Taoyuan)
- **Timezone:** Asia/Taipei

## 🤖 Model Settings

- **Primary AI:** Claude Desktop (Claude Sonnet 4.6) — 取代舊的 Google Antigravity
- **Coding:** GitHub Copilot — 額度快用完（1,500 req/month），2026-04 後重新評估
- **MCP Tools:** obsidian-vault (filesystem) + shell (git 執行)

## 🔐 Credentials Management

- **Storage:** `auth-profiles.json` (Local only)
- **Git Sync:** Handled via `.env` and `.gitignore`
- **Secrets:** Never expose in Markdown or Public Repo

## 📝 User Preferences

- **Editor:** VS Code
- **Terminal:** zsh
- **Git:** Atomic commits, meaningful messages, dev branch for active work
