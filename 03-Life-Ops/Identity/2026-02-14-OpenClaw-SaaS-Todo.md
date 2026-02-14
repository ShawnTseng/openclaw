# OpenClaw SaaS - Cost Validation Plan

> **Created:** 2026-02-14  
> **Status:** Todo  
> **Goal:** Validate business feasibility in 1 week.

---

## 🎯 Phase 1: Cost Structure Validation

### 1️⃣ VPS Pricing Survey

**Goal:** Find a 4 vCPU / 8GB RAM VPS under NT$1,500/month.

**Tasks:**
- [ ] **Linode (Akamai):** Check 4C/8G pricing (TWD).
- [ ] **DigitalOcean:** Check Droplet pricing (TWD).
- [ ] **Vultr:** Check High Frequency Compute pricing (TWD).
- [ ] **Hetzner:** Check European pricing (usually cheapest).
- [ ] **AWS Lightsail:** Check fixed-price instance bundles.

**Success Criteria:** Monthly cost ≤ NT$1,500 (~$50 USD).

---

### 2️⃣ Anthropic API Billing

**Goal:** Confirm actual billing rates for Claude Sonnet 4.5.

**Tasks:**
- [ ] Visit Anthropic Pricing Page.
- [ ] Confirm Input/Output token costs ($3 / $15 per 1M).
- [ ] Calculate TWD exchange rate impact.
- [ ] Check for any minimum spend or hidden fees.

**Success Criteria:** Input ≤ NT$9/1M, Output ≤ NT$45/1M.

---

### 3️⃣ Docker Density Test

**Goal:** Verify how many OpenClaw containers fit on a 4C/8G node.

**Tasks:**
- [ ] Run local Docker stress test:
  ```bash
  docker run --cpus=0.5 --memory=512m openclaw/openclaw:latest
  ```
- [ ] Calculate theoretical capacity:
  - CPU Bound: 4 Cores / 0.5 = 8 containers?
  - RAM Bound: 8GB / 512MB = 16 containers?
- [ ] Estimate system overhead (Nginx, Monitoring).

**Success Criteria:** Support 10-15 concurrent users per node.

---

### 4️⃣ Unit Economics Calculation

**Goal:** Calculate true margin and safety buffer.

**Tasks:**
- [ ] Fill in real VPS costs.
- [ ] Fill in real API costs.
- [ ] Calculate Cost Per User (CPU).
- [ ] Verify Gross Margin > 85%.

**Success Criteria:** Safety Margin > 10x (Price / Cost > 10).

---

## 📅 Timeline

| Task | Est. Time | Priority |
| :--- | :--- | :--- |
| VPS Survey | 30 mins | High |
| API Billing | 20 mins | High |
| Docker Test | 1 hour | Medium |
| Final Calc | 30 mins | High |

**Total:** ~2-3 hours.

---

## ✅ Go/No-Go Decision

- **Go (Phase 2):** If Safety Margin > 10x.
- **No-Go:** If margin is thin or tech stack is too complex.

**Next Step:** Execute VPS survey.
