# OpenClaw

A progressive-loading memory architecture for a long-running personal AI agent, built on Claude Desktop + MCP.

Most personal-agent setups load their entire context file set on every session. That works until the knowledge base outgrows the budget — then every trivial exchange pays for the full corpus. OpenClaw separates context by **access frequency** instead of topic, and routes the rest in on demand.

Boot payload went from 37KB to roughly 6KB with no loss of recall.

- **[AGENTS.md](AGENTS.md)** — the architecture, tier model, and routing mechanism
- **[SOUL.md](SOUL.md)** — persona and operating principles
- **[content/](content/)** — writing and content strategy

Personal data lives in a separate private repository. This one is config, architecture, and portfolio content only.

## Stack

Claude Desktop · MCP (filesystem + shell) · Markdown as the only datastore · git as the only sync layer

---

Built by [Shawn Tseng](https://github.com/ShawnTseng) — .NET / Azure engineer, Melbourne.
