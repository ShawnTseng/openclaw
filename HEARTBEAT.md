# HEARTBEAT.md - Proactive Checks

> **Purpose:** Opportunistic system health checks when messages are received.

## 🕒 Routine Checks (Rotate)
- **Inbox:** Check for urgent emails from Partner Visa or Clients.
- **Calendar:** Next 48h agenda.
- **Health:** Remind Shawn to take medication/water if idle for >4h.
- **System:** Check if `sync-git.sh` ran successfully last night.

## ⚠️ Alerts
- If `sync.log` shows errors, notify immediately.
- If Partner Visa deadline (2026-06-05) < 30 days, escalate priority.
