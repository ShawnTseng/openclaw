# OpenClaw Cron Jobs — Restart Reference

> **Last updated:** 2026-03-22
> **Target model:** GitHub Copilot (Claude Sonnet 4.6)
> **Workspace root:** `/Users/tsengjiyang/.openclaw/workspace`
> **Status:** Paused. Use this file to reconfigure when restarting.

---

## Setup Instructions

Before creating cron jobs, the agent must:
1. Read `SOUL.md` — persona and principles
2. Read `USER.md` — Shawn's preferences, communication style, validation rules
3. Read `memory/MEMORY.md` — current project context

Behavior rules (tone, format, what to prioritize) live in SOUL.md and USER.md.
**Do NOT repeat them inside individual job prompts.**

---

## Directory Paths (updated 2026-03-22)

```
Workspace:     /Users/tsengjiyang/.openclaw/workspace
Daily logs:    workspace/daily/YYYY-MM-DD.md
Long-term mem: workspace/memory/MEMORY.md
Content:       workspace/content/
Projects:      workspace/projects/
Life/Visa:     workspace/life/visa/
Repos:         workspace/repos/  (gitignored, each has own remote)
```

---

## Cron Jobs

### 1. Morning Brief
**Schedule:** `0 7 * * *` (Asia/Taipei)
**Delivery:** Discord announce

```
Morning Brief:

1. Read memory/MEMORY.md — extract P0/P1 active items only
2. Check GitHub activity from yesterday: https://github.com/ShawnTseng
   Repos to check: openclaw, BuddyShopAI
3. Output:

# 🌅 Morning Brief — [Weekday, YYYY-MM-DD]

## Yesterday's Activity
[GitHub commits summary, or "No commits"]

## Today's Top 3
1. [P0] ...
2. [P1] ...
3. [P2] ...

## This Week's Deadlines
[Only items with explicit dates from MEMORY.md]

## 💊 Reminder
Medication + 07:30 run
```

---

### 2. Evening Routine
**Schedule:** `0 22 * * *` (Asia/Taipei)
**Delivery:** Discord announce
**Timeout:** 180s

```
Evening Routine:

1. Check GitHub activity TODAY: https://github.com/ShawnTseng
   Repos: openclaw, BuddyShopAI

2. Write today's daily log to daily/YYYY-MM-DD.md
   Use today's ACTUAL date. Structure:
   - What Happened
   - GitHub Activity
   - Insights
   - Achievements
   - Tomorrow's Focus

3. Send to Discord:
   - 3-sentence summary of today
   - Tomorrow's top priority
   - GitHub link: https://github.com/ShawnTseng/openclaw/blob/main/daily/[TODAY].md
   - Reminder: bedtime 23:00
```

---

### 3. Daily Git Sync
**Schedule:** `0 23 * * *` (no TZ — runs in system time)
**Delivery:** Discord announce

```
Run git sync:
cd /Users/tsengjiyang/.openclaw/workspace
git add -A
git commit -m "chore: daily auto-sync $(date +%Y-%m-%d)" --allow-empty
git push origin main

Report: success or error output.
```

---

### 4. Memory Consolidation
**Schedule:** `0 3 * * *` (Asia/Taipei)
**Delivery:** Discord announce (best effort)

```
Memory Consolidation:

1. Read today's daily log at daily/YYYY-MM-DD.md (yesterday's date)
2. Compare against memory/MEMORY.md
3. If new significant events, decisions, or status changes exist:
   - Update the relevant section in MEMORY.md
   - Remove outdated P2 items (older than 30 days)
4. If nothing significant: do nothing, report "No updates needed"

Do NOT rewrite MEMORY.md wholesale. Surgical updates only.
```

---

### 5. Weekly AI Highlights
**Schedule:** `0 8 * * 0` (Asia/Taipei, Sunday)
**Delivery:** Discord announce
**Timeout:** 120s

```
Weekly AI Highlights:

Search for MAJOR events in the past 7 days only.
Filter criteria (from USER.md):
- New model releases at GPT-5 / Claude 4 level
- API price changes >50%
- Open-source models beating GPT-4
- Major acquisitions or AI regulations

If NO events match: reply "No significant AI news this week."
If events exist: 3–5 bullets max.

Format:
# 📡 Weekly AI Highlights — Week of [date]
[bullets or "No significant news"]
```

---

### 6. Weekly Crypto Analysis
**Schedule:** `0 9 * * 0` (Asia/Taipei, Sunday)
**Delivery:** Discord announce
**Timeout:** 180s

```
Weekly Crypto Market Analysis (week-level signals only):

Search:
- Bitcoin price vs 200 Week Moving Average
- Bitcoin ETF weekly net flows
- Curve 3pool USDT percentage
- Wyckoff phase (accumulation/distribution/markup/markdown)

Score (from USER.md weights):
- 200WMA: 40%
- ETF flows: 30%
- Wyckoff phase: 20%
- USDT peg risk: 10%

Output:
# 🪙 Weekly Crypto Analysis — Week ending [date]

**Score:** [0–100]
**200WMA:** [Above/Below by X%]
**ETF Flows:** [Weekly net]
**Wyckoff Phase:** [phase]

**Signal:** [Hold / Spot Buy Zone / Exit Alert]
**Action:** [One specific sentence]

If no clear signal: "Hold / Continue observation."
```

---

### 7. Weekly Content Refinement
**Schedule:** `0 20 * * 6` (Asia/Taipei, Saturday)
**Delivery:** Discord announce

```
Content Refinement:

1. Read this week's daily logs from daily/
2. Identify the most interesting technical insight or story
3. Draft a blog post outline in content/Drafts/ (filename: NNN-topic-slug.md)
4. Update content/Backlog.md: mark topic as "In Progress"

Target audience: AU engineering market.
Angle: Cloud Platform Engineering, DevOps, or AI workflow lessons.
```

---

### 8. Weekly System Maintenance
**Schedule:** `0 10 * * 0` (Asia/Taipei, Sunday)
**Delivery:** Discord announce

```
Weekly System Maintenance:

1. Security audit: check .gitignore covers memory/, private/, .env
2. Run: brew update && brew outdated (report if >10 packages outdated)
3. Git health: check openclaw and BuddyShopAI repos are clean and synced
4. Report findings in bullet points. Flag anything requiring action.
```

---

### 9. Weekly Self-Reflection
**Schedule:** `0 20 * * 0` (Asia/Taipei, Sunday)  
*(moved from 22:00 to 20:00 to avoid clash with Evening Routine)*
**Delivery:** Discord announce

```
Weekly Self-Reflection:

1. Read this week's daily logs from daily/
2. Assess:
   - What went well?
   - What didn't? Why?
   - Any pattern in what got blocked or delayed?
3. Output: 5–8 bullet points. Be specific, not generic.
   One actionable improvement for next week.
```

---

### 10. Hyperliquid Monthly Check
**Schedule:** `0 20 15 * *` (Asia/Taipei, 15th of each month)
**Delivery:** Discord announce (system event / reminder only)

```
Monthly Hyperliquid Ecosystem Check:

Spend 10–15 minutes reviewing:
1. Hyperliquid Twitter/Discord: new features, partnerships, TVL trend
2. Ecosystem project list: any new on-chain data analysis tools (competitive watch)
3. Community activity: Reddit/Discord sentiment

Update notes in: memory/project-ideas/hyperliquid-whale-tracker.md
Reference: https://hyperliquid.gitbook.io/
```

---

## Disabled / Removed Jobs

| Job | Reason |
|-----|--------|
| Weekly Report | Redundant with Evening Routine + Self-Reflection |
| Morning Briefing Part 2 (AI Signal) | Replaced by Weekly AI Highlights |
| Morning Briefing Part 3 (Crypto Pulse) | Replaced by Weekly Crypto Analysis |

---

## Notes for Restart

- All behavior rules (tone, format, validation) are in SOUL.md and USER.md — don't duplicate in prompts
- Discord channel ID: `1471876205765464296`
- BuddyShopAI and openclaw are the only active repos to monitor
- SeafoodAI repo no longer exists
- `01-Daily/` and `02-Repos/` paths are dead — all updated above
