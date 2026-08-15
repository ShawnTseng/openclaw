# TOOLS.md - Local Configuration Notes

> **Context:** This file stores environment-specific settings for the local OpenClaw instance.
> **Last updated:** 2026-08-02（model 版本更新；其餘沿用 2026-03-22 設定，未逐項重新確認）

## 🏗️ Infrastructure

- **Host:** Shawn's Mac Mini
- **Network:** Home Network (Taoyuan)
- **Timezone:** Asia/Taipei

## 🤖 Model Settings

- **Primary AI:** Claude Desktop (Claude Sonnet 5) — 取代舊的 Google Antigravity；⚠️ 原記錄是 Claude Sonnet 4.6，此版本已不存在，推測為 Sonnet 5，Shawn 可確認實際使用中的型號是否有出入
- **Coding:** GitHub Copilot — 2026-04 那次的額度重新評估結果未記錄，可能需要再確認一次現況
- **MCP Tools:** obsidian-vault (filesystem) + shell (git 執行)

## 🔐 Credentials Management

- **Storage:** `auth-profiles.json` (Local only)
- **Git Sync:** Handled via `.env` and `.gitignore`
- **Secrets:** Never expose in Markdown or Public Repo

## 📝 User Preferences

- **Editor:** VS Code
- **Terminal:** zsh
- **Git:** Atomic commits, meaningful messages, dev branch for active work
