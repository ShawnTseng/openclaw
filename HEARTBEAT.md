# HEARTBEAT.md - Proactive Checks

> **Purpose:** Opportunistic checks when a session starts or a message is received.

## 🕒 Routine Checks (pick one per session, rotate)

- **Visa blocker:** Are passports back from Philippine agent? If yes, flag 英文版產權證明 as next action.
- **BuddyShopAI:** Any client feedback pending? Check `repos/BuddyShopAI` git log.
- **Health:** If Shawn seems to have been working >4h, remind medication/water.
- **Git sync:** Check if `daily/` has an entry for today. If not, remind to log.

## ⚠️ Escalate Immediately

- Visa consultant Mike replies → update `life/visa/` and MEMORY.md
- BuddyShopAI production error → treat as P0
- Mortgage or loan payment due within 7 days
