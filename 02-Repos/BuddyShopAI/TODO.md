# BuddyShopAI - TODO & Feature Backlog

> **Last Updated:** 2026-03-06  
> **Status:** Client Testing Phase (168+ hours silence since Feb 26 deadline)

---

## 🎯 Current Status

**Project Phase:** Client Testing (mrvshop)  
**Deploy Status:** ✅ Production deployed to Azure (East US)  
**Next Milestone:** Client feedback & potential onboarding of additional tenants

---

## 🔴 P0 - Critical (Pre-Production Launch)

### Client Feedback - NEW! 🆕
- [ ] **Template-Based Response Mode (Issue #001)**
  - **Client Requirement:** 100% exact template matching for critical questions (return policy, shipping, etc.)
  - **Solution:** Hybrid mode = Template match first → AI fallback
  - **Effort:** 16-24 hours
  - **Target:** 2026-03-20 (2 weeks)
  - **Details:** See [issues/001-template-based-response.md](issues/001-template-based-response.md)

### LINE Configuration
- [ ] **LINE Webhook URL Setup (Per Tenant)**
  - Set Webhook URL in LINE Developers Console: `https://{tenant}-func.azurewebsites.net/api/linewebhook`
  - Disable "Auto-reply messages"
  - Enable Webhook
  - **Status:** Pending client feedback

### Testing
- [ ] **End-to-End Testing (Per Tenant)**
  - Test with real LINE accounts
  - Verify: FAQ responses, rate limiting, conversation history, timeout reset
  - **Status:** In progress with mrvshop client

### Infrastructure
- [x] ✅ **Key Vault Secret Verification (mrvshop)**
  - Confirmed `kvmrvshopprodt2i` has 3 secrets:
    - AzureOpenAI-ApiKey
    - LINE-ChannelAccessToken
    - LINE-ChannelSecret

---

## 🟡 P1 - High Priority (Near-Term Improvements)

### Content Management
- [ ] **Google Sheets No-Code CMS (Dynamic Events)**
  - **Goal:** Allow store owners to self-edit FAQs without redeployment
  - **Implementation:**
    - Replace static event info in `configs/{tenantId}.json` with Google Sheets
    - Function App reads from public CSV URL
    - Use IMemoryCache (5-10 min) to avoid excessive requests
  - **Value:** Zero-friction content updates
  - **Effort:** ~8-12 hours
  - **Priority:** P1 (high value, medium effort)

### Performance
- [ ] **Cold Start Optimization**
  - **Problem:** Consumption Plan cold starts can take 5-15 seconds
  - **Solutions:**
    - Option A: Use free UptimeRobot to ping every 5 minutes
    - Option B: Accept cold start latency (LINE users are tolerant)
  - **Decision:** Evaluate after client feedback on response time
  - **Effort:** ~2 hours (UptimeRobot setup)

### Content
- [ ] **Expand FAQ Content in `configs/{tenantId}.json`**
  - Based on actual customer questions:
    - Size selection guide (height/weight reference)
    - Payment methods
    - Pre-order process
    - Brand introduction
  - **Effort:** ~4-6 hours per tenant (content research + writing)

### Regional Optimization
- [x] ✅ **Deployment Region Optimization** → Decision Complete
  - **Decision:** Stay in East US, prioritize availability
  - **Rationale:**
    - Japan East: gpt-4o-mini Standard not supported ❌
    - Southeast Asia: Support unknown, needs verification
    - LINE chat is latency-tolerant (1-2 sec acceptable)
    - Re-evaluate ROI after customer growth
  - See [LESSONS_LEARNED.md](doc/development/LESSONS_LEARNED.md)

---

## 🟢 P2 - Medium Priority (Feature Expansion)

### Sales Automation
- [ ] **New Store Onboarding Automation**
  - **Goal:** Standardized process for pitching to new stores
  - **Tools:**
    - Auto web scraper (extract from IG/FB/website)
    - Config generator (convert scraped data to config format)
    - Demo config & presentation docs
  - **Effort:** ~16-20 hours
  - **Priority:** P2 (high value after product-market fit)
  - See [ONBOARDING.md](doc/business/ONBOARDING.md)

### Multi-Platform Support
- [ ] **Instagram Integration (Multi-Platform)**
  - Expand to Instagram Direct Message
  - **Requirements:**
    - Research Instagram Messaging API (needs Meta Business)
    - Design unified message abstraction layer (LINE/IG)
    - Webhook routing by platform
  - **Effort:** ~24-32 hours
  - **Priority:** P2 (depends on market demand)
  - See [INSTAGRAM_INTEGRATION.md](doc/features/INSTAGRAM_INTEGRATION.md)

### Rich Interactions
- [ ] **Rich Menu & Flex Message**
  - **Rich Menu:** Bottom shortcuts (check shipping, track order, contact support)
  - **Flex Message:** Structured card replies (product recommendations, order status)
  - **Effort:** ~8-12 hours
  - **Priority:** P2 (nice-to-have, improves UX)

### Vision Features
- [ ] **Image Search / Visual Recognition (Phase 2)**
  - Use GPT-4o Vision
  - **Use Case:** Customer sends screenshot → AI identifies product → provides link
  - **Note:** Requires upgrade to gpt-4o (higher cost)
  - **Effort:** ~12-16 hours
  - **Priority:** P2 (cost vs. value TBD)

### RAG (Retrieval-Augmented Generation)
- [ ] **PDF Knowledge Base (Phase 2)**
  - Allow store owners to upload brand manuals, fabric care guides
  - **Tech:** Embedding + Vector Search (consider Azure AI Search)
  - **Effort:** ~20-24 hours
  - **Priority:** P2 (premium feature)

### E-Commerce Integration
- [ ] **E-Commerce Platform API Integration (Phase 3)**
  - Integrate with Shopline / Cyberbiz / POS
  - **Features:** Member lookup, order tracking
  - **Effort:** ~32-40 hours per platform
  - **Priority:** P3 (depends on client requirements)

---

## 🔧 Technical Debt

### In-Memory State Limitations
- [ ] **Rate Limiting & Debounce Persistence**
  - **Current:** In-memory Dictionary (resets after instance recycling)
  - **Impact:** Rate limits reset = more lenient for users (acceptable for now)
  - **Solution:** Migrate to Table Storage or Redis if strict enforcement needed
  - **Effort:** ~4-6 hours
  - **Priority:** Low (current behavior acceptable)

### Testing
- [ ] **Unit Tests**
  - **Current:** Zero test coverage
  - **Priority Areas:**
    - `LineSignatureValidator` - Signature verification
    - `ConversationHistoryService` - Debounce logic
    - `PromptProvider` - Knowledge base loading
  - **Effort:** ~8-12 hours
  - **Priority:** Medium (improves maintainability)

### Prompt Engineering
- [ ] **Prompt Optimization**
  - Based on real user conversation data:
    - Response length control
    - Emoji frequency tuning
    - Threshold for escalating to human support
  - **Effort:** Ongoing (2-4 hours per iteration)
  - **Priority:** Medium (iterative improvement)

---

## ✅ Completed

- [x] Azure OpenAI integration (replaced Google Gemini)
- [x] Azure Table Storage for conversation history (replaced IMemoryCache)
- [x] Key Vault + Managed Identity (secure secret management)
- [x] Conversation timeout (24h) & rate limiting (10 questions/hour)
- [x] Message debouncing (3s debounce)
- [x] Consumption Plan deployment (bypassed VM quota limits)
- [x] Resource cleanup (removed unnecessary resources)
- [x] Bicep IaC simplification (4 modules)
- [x] Webhook deployment verification (POST → 401)
- [x] Documentation reorganization
- [x] Multi-tenant platform transformation (MrvShopAI → BuddyShopAI)
- [x] mrvshop first deployment (infra + app, East US)
- [x] Key Vault RBAC role setup
- [x] Deployment region decision (East US, prioritize availability)
- [x] README.md rebranding (Buddy ShopAI)

---

## 📊 Effort Estimation Summary

| Priority | Items | Total Effort (hours) |
|----------|-------|---------------------|
| P0 | 2 pending | ~4-6 (testing) |
| P1 | 3 pending | ~14-20 |
| P2 | 6 features | ~120-152 |
| Tech Debt | 3 items | ~14-22 |
| **Total Backlog** | **14 items** | **~152-200 hours** |

---

## 🎯 Recommended Next Steps (Post-Client Feedback)

### Scenario A: Client Approves (Positive Feedback)
1. **Immediate (Week 1-2):**
   - Complete P0 end-to-end testing
   - Implement Google Sheets CMS (P1, high ROI)
   - Expand FAQ content for mrvshop

2. **Short-term (Week 3-4):**
   - Set up UptimeRobot for cold start mitigation
   - Add unit tests for critical paths
   - Develop new store onboarding automation

3. **Medium-term (Month 2-3):**
   - Pitch to 2-3 new tenants (validate multi-tenant model)
   - Implement Rich Menu/Flex Message
   - Evaluate Instagram integration demand

### Scenario B: Client Requests Changes
1. **Gather Requirements:**
   - Detailed feedback on current functionality
   - Feature gaps or pain points
   - Budget/timeline for enhancements

2. **Prioritize:**
   - Map requests to existing backlog
   - Create new issues if not covered
   - Estimate effort and quote

### Scenario C: Client Silent / Project Pivot
1. **Internal Improvement:**
   - Complete documentation gaps
   - Add unit tests
   - Optimize for demo/showcase

2. **Portfolio Positioning:**
   - Position as "DevOps Showcase" (see 2026-02-17 breakthrough)
   - Highlight: GitOps, Container Apps, Semantic Kernel
   - Prepare case study for future clients

---

## 📝 Notes

### Client Status (mrvshop)
- **Last Contact:** Feb 26, 2026 (deadline day)
- **Silence Duration:** 168+ hours (as of Mar 6)
- **Reassessment Point:** Mar 7, 2026
- **Strategic Patience:** Maintained professional composure; no premature follow-up sent
- **Possible Scenarios:**
  1. Extended internal testing (positive signal)
  2. Approval cycle delays (neutral)
  3. Project pivot/pause (requires reassessment)

### Financial Context
- **Investment:** ~100+ hours development + Azure infrastructure
- **Expected Return:** Deposit + potential ongoing retainer
- **Migration Evidence:** System validates location-independent consulting model
- **Portfolio Value:** Real-world multi-tenant SaaS deployment (regardless of client outcome)

### Strategic Insights
- **Platform Positioning:** Pivot from "fashion e-commerce chatbot" to "multi-tenant AI customer service platform"
- **Market Validation:** Need 2-3 paying customers to prove business model
- **Technology Showcase:** Strong foundation for Australia consulting portfolio
- **Lessons Learned:** Document deployment decisions for future client pitches

---

## 🔗 Related Documents

- [Development Roadmap](doc/development/ROADMAP.md) - Detailed feature roadmap
- [Lessons Learned](doc/development/LESSONS_LEARNED.md) - Technical decisions
- [Business Model](doc/business/BUSINESS_MODEL.md) - Positioning & market
- [Onboarding](doc/business/ONBOARDING.md) - New customer SOP
- [Architecture Overview](doc/architecture/OVERVIEW.md) - System design

---

> **Last Review:** 2026-03-06  
> **Next Review:** After client feedback (target: Mar 7)  
> **Owner:** Shawn Tseng
