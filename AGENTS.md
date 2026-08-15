# AGENTS.md — Workspace Architecture

A two-repo, progressive-loading memory system for a long-running personal AI agent. The design goal is a **flat token cost per session regardless of how much the knowledge base grows**.

## The Problem

Naive agent memory loads every context file at session start. As the knowledge base grows, every conversation — including "what's the weather" — pays for the full corpus. In this workspace the boot payload had reached 37KB, of which roughly 85% was reference material used a few times a month.

## The Design

Three tiers, separated by **access frequency**, not by topic:

```
Tier 0  BOOT.md          ~4KB   every session
Tier 1  memory/*.md      ~5-10KB each, loaded when a trigger word hits
Tier 2  memory/archive/  never auto-loaded; historical record only
```

`BOOT.md` holds only what the agent would get *wrong* without it: current life state, active work, behavioral rules, and a **routing table** mapping conversation topics to Tier 1 files. Everything else is a lookup.

The routing table is the key mechanism. It costs ~600 bytes and replaces ~25KB of preloaded reference.

## Repo Split

```
public/             PUBLIC  — persona, architecture, portfolio content
├── SOUL.md              persona and operating principles
├── AGENTS.md            this file
├── README.md
├── content/             articles, resume, content strategy
└── repos/               source code (gitignored, independent remotes)

private/    PRIVATE — everything personal
├── BOOT.md              Tier 0: the only file loaded every session
├── USER.md              background, on demand
├── TOOLS.md             local environment config
├── HEARTBEAT.md         proactive checks
├── memory/              Tier 1 + Tier 2
├── daily/               YYYY-MM-DD.md raw logs
└── ops/                 automation scripts and maintenance protocols
```

The split rule is mechanical: **if a file contains a fact about a real person, it lives in private.** No judgement calls.

## Protocols

- **Write:** long-term facts → the matching `memory/` file, surgical edits only. Raw daily events → `daily/YYYY-MM-DD.md`. Expired content → `memory/archive/`.
- **Meditation:** a scheduled refinement pass that re-sorts content between tiers and prunes decayed facts. See `ops/MEDITATION.md`.
- **Sync:** both repos push daily via `ops/daily-push.sh`.
- **Security:** the public `.gitignore` hard-blocks every private filename by name, not just by directory.

## Results

| | Before | After |
|---|---|---|
| Boot payload | 37.2 KB | ~6 KB |
| Files read at boot | 4 | 2 |
| Reference material preloaded | ~25 KB | 0 |
