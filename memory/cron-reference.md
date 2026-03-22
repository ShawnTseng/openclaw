# OpenClaw Cron Jobs — Restart Reference

> **Last updated:** 2026-03-23
> **Target:** Cowork Scheduled Tasks (Claude Sonnet 4.6)
> **Workspace:** `/Users/tsengjiyang/Desktop/projects/openclaw`
> **Status:** Ready to configure. Use this file to set up Cowork tasks.

---

## Setup

Each task must boot with:
1. Read `SOUL.md` — persona
2. Read `USER.md` — preferences and validation rules
3. Read `memory/MEMORY.md` — current context

Tone, format, and validation rules live in SOUL.md / USER.md. Do not repeat them in job prompts.

---

## Paths

```
Workspace:  /Users/tsengjiyang/Desktop/projects/openclaw
Daily logs: daily/YYYY-MM-DD.md
Memory:     memory/MEMORY.md
Content:    content/Drafts/
Projects:   projects/
Visa:       life/visa/
Repos:      repos/ (gitignored, each has own remote)
GitHub:     https://github.com/ShawnTseng
```

---

## Jobs (4 total)

---

### 1. Daily Wrap
**Schedule:** Every day at 23:00 (Asia/Taipei)

```
Daily Wrap:

1. Boot: read SOUL.md, USER.md, memory/MEMORY.md

2. Check today's GitHub activity at https://github.com/ShawnTseng
   Repos: openclaw, BuddyShopAI
   Note commits, PRs, issues from today only.

3. Write daily/YYYY-MM-DD.md (use today's actual date):
   ## What Happened
   [Key events, decisions, blockers]

   ## GitHub Activity
   [Commits summary or "No commits"]

   ## Insights
   [1–2 observations worth keeping]

   ## Tomorrow's Focus
   [Top 1–2 priorities from MEMORY.md]

4. Update memory/MEMORY.md if needed:
   - New project status changes → update P1
   - Completed P2 items or items older than 30 days → remove
   - Surgical edits only. Do NOT rewrite wholesale.
   - If nothing changed: skip.

5. Git sync:
   cd /Users/tsengjiyang/Desktop/projects/openclaw
   git add -A
   git commit -m "chore: daily wrap $(date +%Y-%m-%d)" --allow-empty
   git push origin main

6. Report to Discord: one-line status + git result.
```

---

### 2. Weekly Intelligence
**Schedule:** Every Sunday at 09:00 (Asia/Taipei)

```
Weekly Intelligence:

1. Boot: read USER.md for filter criteria.

2. AI Highlights — search past 7 days for MAJOR events only:
   Filter (from USER.md): GPT-5/Claude 4-level releases, API price changes >50%,
   open-source models beating GPT-4, major acquisitions or AI regulations.
   If nothing qualifies: "No significant AI news this week."

3. Crypto Analysis — search:
   - Bitcoin price vs 200 Week Moving Average
   - Bitcoin ETF weekly net flows
   - Curve 3pool USDT %
   - Wyckoff phase

   Score (from USER.md): 200WMA 40% + ETF flows 30% + Wyckoff 20% + USDT risk 10%

4. Output:

# 📡 Weekly Intelligence — [Week of YYYY-MM-DD]

## AI
[3–5 bullets or "No significant news"]

## Crypto
**Score:** [0–100]
**200WMA:** [Above/Below X%]
**ETF Flows:** [Weekly net]
**Wyckoff:** [Phase]
**Signal:** [Hold / Spot Buy Zone / Exit Alert]
```

---

### 3. Weekly Review
**Schedule:** Every Sunday at 20:00 (Asia/Taipei)

```
Weekly Review:

1. Boot: read memory/MEMORY.md and this week's daily/ logs.

2. Reflection:
   - What actually got done this week?
   - What got blocked or delayed? Why?
   - Any pattern worth noting?
   Output: 4–6 specific bullets. Avoid generic observations.

3. Content — identify the most interesting technical story from this week's logs:
   - Angle: Cloud Platform Engineering, DevOps, or AI workflow lessons
   - Audience: AU engineering market
   - Create draft outline in content/Drafts/NNN-topic-slug.md
   - Update content/Backlog.md: mark as "In Progress"
   - If nothing worth writing this week: skip and say why.

4. Output both sections to Discord.
```

---

### 4. Weekly System Check
**Schedule:** Every Sunday at 10:00 (Asia/Taipei)

```
Weekly System Check:

1. Security: verify .gitignore covers memory/, private/, .env files.
   Flag any sensitive files that appear tracked.

2. Packages: run `brew update && brew outdated`
   Report only if >5 packages outdated or any security-related updates.

3. Git health:
   - openclaw: git status, confirm clean and synced with origin
   - BuddyShopAI (repos/BuddyShopAI): git status
   Flag anything not pushed.

4. Report: bullet list. Flag items needing action, skip noise.
```

---

### 5. Hyperliquid Monthly Check
**Schedule:** 15th of each month at 20:00 (Asia/Taipei)

```
Hyperliquid Monthly Check (10 minutes):

Check:
1. Hyperliquid Twitter/Discord: new features, partnerships, TVL trend
2. Ecosystem: any new on-chain data analysis tools (competitive watch)
3. Community: any major sentiment shifts

Save brief notes to memory/project-ideas/hyperliquid-whale-tracker.md
Format: date + 3–5 bullet observations.
Reference: https://hyperliquid.gitbook.io/
```

---

## Discord

Channel ID: `1471876205765464296`

---

## Removed Jobs

| Job | Reason |
|-----|--------|
| Morning Brief | Replaced by on-demand new conversation |
| Evening Routine (separate) | Merged into Daily Wrap |
| Memory Consolidation (separate) | Merged into Daily Wrap |
| Daily Git Sync (separate) | Merged into Daily Wrap |
| Weekly Self-Reflection (separate) | Merged into Weekly Review |
| Weekly Content Refinement (separate) | Merged into Weekly Review |
| Weekly Report | Was already disabled |
| AI Signal (daily) | Replaced by Weekly Intelligence |
| Crypto Pulse (daily) | Replaced by Weekly Intelligence |
