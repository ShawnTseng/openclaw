# Issue #001 - Template-Based Response Mode

> **Type:** Feature Request (Client Feedback)  
> **Priority:** P0 (Blocker for client approval)  
> **Reporter:** mrvshop (Client)  
> **Date:** 2026-03-06

---

## 📋 Problem Statement

**Current Behavior:**
- System uses Azure OpenAI (gpt-4o-mini) to generate AI responses
- Responses are dynamic and non-deterministic
- Client cannot guarantee exact wording for critical messages (e.g., return policy, shipping info)

**Client Requirement:**
- For specific question types, need **100% template-based answers**
- Must match exact wording from pre-defined templates
- Zero AI hallucination risk for legal/policy content

---

## 🎯 Proposed Solution

### Architecture Change: Hybrid Response Mode

```
User Question
    ↓
Intent Classification (AI or Rule-based)
    ↓
    ├─► Template Match Found? → Return Template (100% accurate)
    └─► No Match → AI Generation (Current behavior)
```

### Implementation Options

#### Option A: Intent + Template Mapping (Recommended)
```json
{
  "templates": [
    {
      "intent": "return_policy",
      "triggers": ["退貨", "退款", "換貨", "七天鑑賞"],
      "template": "【退換貨政策】\n七天鑑賞期：收到商品後7天內可退換貨...",
      "mode": "exact"
    },
    {
      "intent": "shipping_time",
      "triggers": ["出貨", "到貨", "配送", "幾天"],
      "template": "【配送時間】\n訂單確認後1-3個工作天出貨...",
      "mode": "exact"
    }
  ]
}
```

**Pros:**
- Precise control over critical responses
- Simple to implement (keyword matching or embedding similarity)
- Fast response time (no LLM call for template matches)

**Cons:**
- Requires manual template creation per tenant
- Need to maintain trigger keyword list

---

#### Option B: Strict RAG (Retrieval-Augmented Generation)
- Upload templates to vector database
- Semantic search for matching template
- If high similarity (>0.9), return template verbatim
- If low similarity, use AI generation

**Pros:**
- More flexible matching (handles paraphrasing)
- Easier to scale (just add documents)

**Cons:**
- Requires vector database (additional cost)
- Embedding API calls (latency + cost)
- Risk of false positives

---

#### Option C: AI Instruction Enforcement
- Add strict system prompt: "For questions about X, Y, Z, respond with EXACTLY the following templates..."
- Provide all templates in system prompt

**Pros:**
- Minimal code change

**Cons:**
- ❌ **Cannot guarantee 100% exactness** (LLM may still paraphrase)
- Token cost increases (long system prompt)
- Not reliable for legal/compliance use cases

---

## 🏗️ Recommended Architecture (Option A)

### Step 1: Config Schema Update

**Add to `configs/{tenantId}.json`:**

```json
{
  "storeInfo": { /* existing */ },
  "knowledge": { /* existing */ },
  "templates": {
    "mode": "hybrid",  // "hybrid" | "template-only" | "ai-only"
    "items": [
      {
        "id": "return_policy",
        "triggers": ["退貨", "退款", "換貨", "七天鑑賞", "不滿意"],
        "response": "【退換貨政策】\n七天鑑賞期：收到商品後7天內可申請退換貨（商品需保持全新未使用）。\n退貨流程：LINE 私訊客服 → 提供訂單編號 → 寄回商品 → 收到後3-5個工作天退款。",
        "responseType": "exact"  // "exact" | "ai-enhanced"
      },
      {
        "id": "shipping_cost",
        "triggers": ["運費", "郵資", "免運", "shipping"],
        "response": "【運費說明】\n單筆訂單滿 $1200 免運 🎉\n未滿則酌收運費 $80（超商取貨）或 $100（宅配）",
        "responseType": "exact"
      }
    ]
  }
}
```

---

### Step 2: Code Implementation

**New Service: `TemplateMatchingService.cs`**

```csharp
public class TemplateMatchingService
{
    public TemplateMatchResult? TryMatch(string userMessage, List<Template> templates)
    {
        foreach (var template in templates)
        {
            if (template.Triggers.Any(trigger => 
                userMessage.Contains(trigger, StringComparison.OrdinalIgnoreCase)))
            {
                return new TemplateMatchResult
                {
                    TemplateId = template.Id,
                    Response = template.Response,
                    Confidence = 1.0 // Exact keyword match
                };
            }
        }
        return null;
    }
}
```

**Updated `LineWebhook.cs` Flow:**

```csharp
// 1. Load tenant config (with templates)
var config = await _configProvider.GetConfigAsync(tenantId);

// 2. Try template match first
var templateMatch = _templateMatchingService.TryMatch(userMessage, config.Templates.Items);

string replyText;
if (templateMatch != null && config.Templates.Mode != "ai-only")
{
    // Use template
    replyText = templateMatch.Response;
    _logger.LogInformation("Template match: {TemplateId}", templateMatch.TemplateId);
}
else if (config.Templates.Mode == "template-only")
{
    // Template-only mode: no AI fallback
    replyText = "抱歉，這個問題我需要請真人客服回答您 🙏";
}
else
{
    // Fallback to AI generation
    var history = await _historyService.GetHistoryAsync(tenantId, userId);
    var prompt = _promptProvider.BuildPrompt(config, history);
    replyText = await _kernel.InvokePromptAsync<string>(prompt);
}
```

---

### Step 3: Migration Path

**Phase 1: Backward Compatible (Week 1)**
- Add optional `templates` section to config schema
- If `templates` section missing, default to pure AI mode
- mrvshop can test with 3-5 critical templates

**Phase 2: Client Testing (Week 2)**
- mrvshop provides full template list
- Test hybrid mode in production
- Collect metrics: template hit rate, fallback rate

**Phase 3: Documentation (Week 3)**
- Update [CONFIGURATION.md](../guides/CONFIGURATION.md) with template examples
- Create template authoring guide
- Add template testing script

---

## 📊 Impact Analysis

### Pros
✅ **Client Control:** 100% guarantee on critical responses  
✅ **Legal Safety:** No hallucination risk for policies  
✅ **Cost Savings:** Template matches skip LLM calls (save ~$0.0001/msg)  
✅ **Speed:** Keyword matching < 1ms (vs. 500-1000ms for LLM)  
✅ **Backward Compatible:** Existing tenants unaffected

### Cons
⚠️ **Maintenance:** Store owners need to create/update templates  
⚠️ **Complexity:** Two code paths (template + AI)  
⚠️ **Trigger Coverage:** Poorly defined triggers = template misses

### Metrics to Track
- **Template Hit Rate:** % of questions matched by templates
- **Fallback Rate:** % using AI generation
- **Response Time:** Template vs. AI latency
- **Customer Satisfaction:** Do users prefer exact templates?

---

## 🎯 Success Criteria

1. ✅ mrvshop can define 10+ templates in config
2. ✅ Keyword triggers successfully match 80%+ of intended questions
3. ✅ Template responses return in <100ms
4. ✅ AI fallback still works for non-template questions
5. ✅ Client confirms template accuracy = 100%

---

## 🚀 Implementation Plan

### Week 1: Core Development (8-12 hours)
- [ ] Design template schema
- [ ] Implement `TemplateMatchingService`
- [ ] Update `LineWebhook.cs` to support hybrid mode
- [ ] Add unit tests for template matching
- [ ] Update mrvshop config with 3 sample templates

### Week 2: Testing & Refinement (4-6 hours)
- [ ] Deploy to mrvshop prod
- [ ] Collect client feedback on template matches
- [ ] Refine trigger keywords based on miss rate
- [ ] Add logging/telemetry for template hit rate

### Week 3: Documentation & Rollout (4-6 hours)
- [ ] Update configuration docs
- [ ] Create template authoring guide
- [ ] Prepare for additional tenant onboarding
- [ ] Write blog post on hybrid AI architecture

**Total Effort:** 16-24 hours

---

## 🔗 Related Issues

- #002 (TBD) - Template authoring UI (future enhancement)
- #003 (TBD) - Template A/B testing (analytics)
- See also: [Google Sheets CMS](../TODO.md#p1---high-priority) (could integrate with template management)

---

## 📝 Client Communication

**To mrvshop:**

> 感謝您的反饋！我們理解您需要對某些重要回答（如退貨政策、運費說明）有 100% 精確控制。
>
> 我們規劃了一個「混合式回答模式」：
> - **模板匹配：** 對於您預先定義的問題，系統會回傳固定模板（一字不差）
> - **AI 補充：** 對於模板沒有涵蓋的問題，仍然使用 AI 生成回答
>
> 這樣既保證重要內容的準確性，又保留 AI 的靈活性。
>
> 預計開發時間 2-3 週，會先提供 demo 版本給您測試。

---

> **Status:** Open  
> **Assignee:** Shawn Tseng  
> **Target Completion:** 2026-03-20 (2 weeks)
