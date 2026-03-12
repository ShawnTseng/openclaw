# BuddyShopAI - TODO & Development Backlog

> **Last Updated:** 2026-03-12  
> **Status:** Production Deployed | Awaiting Client Feedback (120+ hours since Mar 6 deployment)

---

## 🎯 Current Status

**Project Phase:** Post-Deployment Testing (mrvshop)  
**Deploy Status:** ✅ Production deployed to Azure (East US)  
**Latest Deploy:** 2026-03-06 (Template Response System)  
**Client Response:** Pending (120+ hours silence)

---

## 🔴 P0 - Critical (Immediate)

### Dashboard MVP (NEW Priority - Mar 10)
- [ ] **Simple Monitoring Dashboard**
  - **Client Pain Point:** Manual command-line checks at 12:00 & 16:00 daily
  - **Business Impact:** Client's workflow = AI automation + manual exception review
  - **Core Features:**
    - Recent N hours conversation list (timestamp, customer, question summary, AI response status)
    - Flag conversations needing human intervention (keyword triggers, dissatisfaction signals, complex queries)
    - One-click handoff (AI → Human mode)
    - Basic stats (daily conversations, AI handling rate, human intervention rate)
    - Mobile-friendly UI (client likely checks on phone)
  - **Technical Approach:** Web interface reading LINE webhook logs + AI response logs (static page + API, no complex backend)
  - **Strategic Value:** Natural product evolution (CLI → Dashboard) for B2B SaaS
  - **Effort:** ~12-16 hours
  - **Priority:** P0 (validates product-market fit and growth path)
  - **Status:** Pending client feature discussion

### Client Feedback Response
- [ ] **Bug Fixes / Adjustment Requests**
  - Monitor for client feedback on March 6 deployment
  - Priority response window: <4 hours
  - **Status:** On standby

---

## 🟢 P1 - High Priority (Near-Term)

### Multi-Admin Support (Dashboard Phase 2)
- [ ] **Permission Management**
  - Multiple admin accounts with different permission levels
  - Admin scheduling (who's on-call for human handoff)
  - Admin activity logs
  - **Effort:** ~8-12 hours
  - **Priority:** P1 (after dashboard MVP validated)

### Multiple AI Configurations
- [ ] **Personality/Response Style Switching**
  - Different AI tones for different customer segments
  - A/B testing for response effectiveness
  - Configuration hot-reload
  - **Effort:** ~6-8 hours
  - **Priority:** P1 (enhances platform flexibility)

### Content Management
- [ ] **Google Sheets No-Code CMS**
  - Allow store owners to edit FAQs without redeployment
  - Replace static content in `configs/{tenantId}.json`
  - Function App reads from public CSV URL
  - Cache with IMemoryCache (5-10 min)
  - **Value:** Zero-friction content updates
  - **Effort:** ~8-12 hours
  - **Priority:** P1 (high value, medium effort)

### Performance
- [ ] **Cold Start Optimization**
  - **Problem:** Consumption Plan cold starts 5-15 seconds
  - **Solutions:**
    - Option A: UptimeRobot ping every 5 minutes
    - Option B: Accept cold start (LINE users tolerant)
  - **Decision:** Evaluate after client feedback
  - **Effort:** ~2 hours (UptimeRobot setup)

---

## 🟡 P2 - Medium Priority (Feature Expansion)

### Business Model Exploration
- [ ] **Open-Source Core + Premium Features**
  - **Options:**
    - Freemium model (basic free, premium paid)
    - Pure subscription SaaS
  - **Decision:** After 2-3 paying customers validate model
  - **Effort:** ~16-24 hours (business model + licensing)

### Sales Automation
- [ ] **New Store Onboarding Automation**
  - Auto web scraper (extract from IG/FB/website)
  - Config generator (scraped data → config format)
  - Demo config & presentation docs
  - **Effort:** ~16-20 hours
  - **Priority:** P2 (after product-market fit)

### Multi-Platform Support
- [ ] **Instagram Integration**
  - Instagram Direct Message support
  - Meta Business Account requirements research
  - Unified message abstraction layer (LINE/IG)
  - **Effort:** ~24-32 hours
  - **Priority:** P2 (depends on market demand)

### Rich Interactions
- [ ] **Rich Menu & Flex Message**
  - Rich Menu: Bottom shortcuts (check shipping, track order, support)
  - Flex Message: Structured cards (product recommendations, order status)
  - **Effort:** ~8-12 hours
  - **Priority:** P2 (UX improvement)

### Vision Features
- [ ] **Image Search / Visual Recognition**
  - GPT-4o Vision integration
  - Use Case: Customer sends screenshot → AI identifies product → provides link
  - Note: Requires gpt-4o upgrade (higher cost)
  - **Effort:** ~12-16 hours
  - **Priority:** P2 (cost vs. value TBD)

### RAG (Retrieval-Augmented Generation)
- [ ] **PDF Knowledge Base**
  - Upload brand manuals, fabric care guides
  - Tech: Embedding + Vector Search (Azure AI Search)
  - **Effort:** ~20-24 hours
  - **Priority:** P2 (premium feature)

### E-Commerce Integration
- [ ] **E-Commerce Platform API**
  - Integrate with Shopline / Cyberbiz / POS
  - Features: Member lookup, order tracking
  - **Effort:** ~32-40 hours per platform
  - **Priority:** P3 (depends on client requirements)

---

## 🔧 Technical Debt

### Testing
- [ ] **Unit Tests**
  - **Priority Areas:**
    - `LineSignatureValidator` - Signature verification
    - `ConversationHistoryService` - Debounce logic
    - `PromptProvider` - Knowledge base loading
    - Template matching logic
  - **Effort:** ~8-12 hours
  - **Priority:** Medium (improves maintainability)

### Prompt Engineering
- [ ] **Prompt Optimization**
  - Based on real user conversation data:
    - Response length control
    - Emoji frequency tuning
    - Human escalation threshold
  - **Effort:** Ongoing (2-4 hours per iteration)
  - **Priority:** Medium (iterative improvement)

### In-Memory State Limitations
- [ ] **Rate Limiting Persistence**
  - Current: In-memory Dictionary (resets after instance recycling)
  - Impact: Rate limits reset = more lenient (acceptable for now)
  - Solution: Migrate to Table Storage or Redis if strict enforcement needed
  - **Effort:** ~4-6 hours
  - **Priority:** Low (current behavior acceptable)

---

## ✅ Recently Completed (March 2026)

### Template Response System (Issue #001) - Mar 6
- [x] ✅ **100% Exact Template Matching**
  - Implemented: 20+ FAQ templates with `[範本]` tags
  - Multi-admin support (mrvshop, mrvshop-joyce)
  - QA automation with auto-handoff
  - `[SPLIT]` mechanism for multi-message responses
  - Production deployment completed
  - **Result:** 29 commits, 4.5-5 hours (vs 16-24h estimate)
  - **Technical Insight:** Prompt engineering > complex code logic

### Previous Milestones
- [x] Azure OpenAI integration (replaced Google Gemini)
- [x] Azure Table Storage for conversation history
- [x] Key Vault + Managed Identity
- [x] Conversation timeout (24h) & rate limiting (10q/hour)
- [x] Message debouncing (3s)
- [x] Consumption Plan deployment
- [x] Bicep IaC simplification (4 modules)
- [x] Multi-tenant platform transformation (MrvShopAI → BuddyShopAI)
- [x] mrvshop first deployment (East US)

---

## 📊 Effort Estimation Summary

| Priority | Items | Total Effort (hours) |
|----------|-------|---------------------|
| P0 | 2 pending | ~12-20 |
| P1 | 4 pending | ~24-34 |
| P2 | 7 features | ~132-172 |
| Tech Debt | 3 items | ~14-22 |
| **Total Backlog** | **16 items** | **~182-248 hours** |

---

## 🎯 Next Actions (Prioritized)

### Immediate (This Week)
1. **Await Client Feedback** on March 6 template deployment
2. **Dashboard MVP Discussion** (validate feature request)
3. **Bug Fixes** if client reports issues (<4h response window)

### Short-Term (Week 2-3)
1. **Implement Dashboard MVP** (if client confirms need)
2. **Google Sheets CMS** (high ROI, low effort)
3. **UptimeRobot Setup** (cold start mitigation)

### Medium-Term (Month 2-3)
1. **Multi-admin permission system** (dashboard phase 2)
2. **New store onboarding automation** (sales process)
3. **Unit test coverage** (critical paths)

### Strategic Evaluation Points
- **After Client Feedback:** Prioritize P0/P1 based on real usage patterns
- **After Dashboard MVP:** Evaluate business model (open-source vs SaaS)
- **After 2-3 Customers:** Validate multi-tenant model and pricing

---

## 📝 Notes

### Development Philosophy (Mar 6 Lesson)
- **Prompt Engineering > Code Logic** when applicable
- Config-driven templates: lighter, more maintainable, easier to extend
- Always verify git log for actual work vs perceived effort
- 3-4x speedup possible with right approach

### Client Context (mrvshop)
- **Last Deployment:** Mar 6, 2026 (major feature release)
- **Client Silence:** 120+ hours (as of Mar 12)
- **Response Window:** 4-hour SLA for urgent communications
- **Remote Work Validation:** 11-day Japan trip (264+ automated checks, 100% success)
- **Weekend Operations:** 60+ automated checks (Mar 7-8), zero manual interventions

### Strategic Value
- **Portfolio Evidence:** Multi-tenant SaaS deployment at production scale
- **Partner Visa Evidence:** Location-independent consulting model validated
- **Tech Stack Showcase:** Azure, .NET 8, Semantic Kernel, OpenAI
- **Business Model Potential:** Validates AI automation market demand

### 11-Day Travel Validation (Feb 23 - Mar 5)
- Remote monitoring: Zero alerts, zero errors, zero interventions
- 264+ automated heartbeat checks with 100% success rate
- 4-hour response window: 100% compliance maintained
- **Result:** Location-independent work model validated at production scale

---

## 🔗 Related Documents

- [Development Roadmap](doc/development/ROADMAP.md) - Detailed feature roadmap
- [Lessons Learned](doc/development/LESSONS_LEARNED.md) - Technical decisions
- [Business Model](doc/business/BUSINESS_MODEL.md) - Positioning & market
- [Onboarding](doc/business/ONBOARDING.md) - New customer SOP
- [Architecture Overview](doc/architecture/OVERVIEW.md) - System design
- [Issue #001](issues/001-template-based-response.md) - Template response implementation

---

> **Last Review:** 2026-03-12  
> **Next Review:** After client feedback or weekly sprint planning  
> **Owner:** Shawn Tseng  
> **Branch:** `dev` (all development work)  
> **Development:** VS Code + GitHub Copilot
