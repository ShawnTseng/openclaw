# OpenClaw Cron Jobs — Restart Reference

> **Last updated:** 2026-03-23
> **Target:** Cowork Scheduled Tasks (Claude Sonnet 4.6)
> **Workspace:** `/Users/tsengjiyang/Desktop/projects/openclaw`
> **Status:** Active — 4 tasks configured in Cowork.

---

## Setup

Each task boots with:
1. Read `SOUL.md` — persona
2. Read `USER.md` — preferences and validation rules
3. Read `memory/MEMORY.md` — current context

Tone, format, and validation rules live in SOUL.md / USER.md. Do not repeat in job prompts.

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

## Active Jobs (4)

### 1. Daily Wrap
**Schedule:** Every day at 23:00 (Asia/Taipei)

```
Read these files first:
- /Users/tsengjiyang/Desktop/projects/openclaw/SOUL.md
- /Users/tsengjiyang/Desktop/projects/openclaw/USER.md
- /Users/tsengjiyang/Desktop/projects/openclaw/memory/MEMORY.md

Then:

1. Check today's GitHub activity at https://github.com/ShawnTseng
   (repos: openclaw, BuddyShopAI)

2. Write today's daily log to /Users/tsengjiyang/Desktop/projects/openclaw/daily/YYYY-MM-DD.md
   (use today's actual date):
   ## What Happened
   ## GitHub Activity
   ## Insights
   ## Tomorrow's Focus (based on MEMORY.md P0/P1 only)

3. Update /Users/tsengjiyang/Desktop/projects/openclaw/memory/MEMORY.md if needed.
   Surgical edits only — update changed project statuses, remove P2 items older than 30 days.
   Skip entirely if nothing significant changed.

4. Run in terminal:
   cd /Users/tsengjiyang/Desktop/projects/openclaw && git add -A && git commit -m "chore: daily wrap $(date +%Y-%m-%d)" --allow-empty && git push origin main

5. Report one-line completion status.
```

---

### 2. Weekly Intelligence
**Schedule:** Every Sunday at 09:00 (Asia/Taipei)

```
Read /Users/tsengjiyang/Desktop/projects/openclaw/USER.md for filter criteria.

1. AI Highlights — search past 7 days for MAJOR events only:
   Filter: GPT-5/Claude 4-level releases, API price changes >50%,
   open-source models beating GPT-4, major AI acquisitions or regulations.
   If nothing qualifies: "No significant AI news this week."

2. Crypto Analysis — search:
   - Bitcoin price vs 200 Week Moving Average
   - Bitcoin ETF weekly net flows
   - Curve 3pool USDT %
   - Wyckoff phase (accumulation/distribution/markup/markdown)
   Score: 200WMA 40% + ETF flows 30% + Wyckoff 20% + USDT risk 10%

Output:
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
Read these files:
- /Users/tsengjiyang/Desktop/projects/openclaw/memory/MEMORY.md
- This week's files in /Users/tsengjiyang/Desktop/projects/openclaw/daily/

1. Reflection — be specific, not generic:
   - What actually got done this week?
   - What got blocked or delayed, and why?
   - One pattern worth noting?
   Output: 4–6 bullets.

2. Content — find the most interesting technical story from this week's logs:
   - Angle: Cloud Platform Engineering, DevOps, or AI workflow lessons
   - Audience: AU engineering market
   - Create outline in /Users/tsengjiyang/Desktop/projects/openclaw/content/Drafts/NNN-topic-slug.md
   - Update /Users/tsengjiyang/Desktop/projects/openclaw/content/Backlog.md: mark as "In Progress"
   - If nothing worth writing: skip and explain why.

3. Output both sections.
```

---

### 4. Weekly System Check
**Schedule:** Every Sunday at 10:00 (Asia/Taipei)

```
1. Security: check /Users/tsengjiyang/Desktop/projects/openclaw/.gitignore
   covers memory/, private/, .env — flag if any sensitive files appear tracked.

2. Packages: run `brew update && brew outdated`
   Report only if >5 packages outdated or any security-related updates.

3. Git health:
   - cd /Users/tsengjiyang/Desktop/projects/openclaw && git status
   - cd /Users/tsengjiyang/Desktop/projects/openclaw/repos/BuddyShopAI && git status
   Flag anything unpushed.

4. Report as bullet list. Flag items needing action, skip noise.
```

---

## Discord

Channel ID: `1471876205765464296`
