# 📱 Instagram 平台整合計劃

> Buddy ShopAI 多平台擴展：Instagram Direct Message 支援  
> 最後更新：2026-02-13

---

## 🎯 目標

將 Buddy ShopAI 從單一 LINE 平台擴展至支援 **Instagram Direct Message (DM)**，  
讓服飾電商客戶可以在 Instagram 上也能提供 AI 智慧客服，提升多平台客戶體驗。

---

## 📊 為什麼要支援 Instagram？

### 市場需求

| 觀察 | 數據/說明 |
|------|----------|
| Instagram 在台灣普及率 | 超過 900 萬活躍用戶（2025） |
| 服飾電商主要導流平台 | Instagram > Facebook > LINE（年輕族群） |
| 客戶使用習慣 | 許多消費者習慣在 IG DM 詢問商品細節 |
| 競爭優勢 | 目前市面上少有同時支援 LINE + IG 的 AI 客服 |

### 客戶痛點

- **分散式客服管理**：LINE 與 IG 分別回覆，效率低
- **IG DM 回覆慢**：晚上或假日沒人值班
- **FAQ 重複回答**：在 IG 上也要一直回答運費、退貨等問題

---

## 🏗️ 技術架構設計

### 方案比較

| 方案 | 優點 | 缺點 | 建議 |
|------|------|------|------|
| **Meta Graph API** | 官方正式 API、穩定可靠 | 需申請 Meta Business、審核嚴格 | ✅ 推薦（正式環境） |
| **Instagram Basic Display API** | 簡單易用 | 無法回覆訊息（僅讀取） | ❌ 不適用 |
| **第三方服務（如 Chatfuel）** | 無需開發 | 成本高、客製化受限 | ⚠️ 備選（快速驗證） |

### 採用方案：Meta Graph API

使用 **Instagram Messaging API**（Meta Graph API 的一部分）來接收與回覆 Instagram DM。

---

## 🔧 Instagram Messaging API 基礎

### 前置需求

1. **Meta Business 帳號**  
   - 建立 Meta Business 帳號（免費）
   - 連結 Instagram 專業帳號（Creator / Business）

2. **Meta App 與權限**  
   - 在 Meta for Developers 建立 App
   - 申請 `instagram_manage_messages` 權限（需審核）

3. **Webhook 訂閱**  
   - 設定 Webhook URL 接收訊息事件
   - 驗證 Webhook（Meta 會發送 GET 請求驗證）

### API 流程對比：LINE vs Instagram

| 步驟 | LINE Messaging API | Instagram Messaging API |
|------|-------------------|------------------------|
| **接收訊息** | Webhook POST `/api/linewebhook` | Webhook POST `/api/igwebhook` |
| **訊息格式** | `events[].message.text` | `entry[].messaging[].message.text` |
| **回覆訊息** | `POST https://api.line.me/v2/bot/message/reply` | `POST https://graph.facebook.com/v18.0/me/messages` |
| **驗證簽章** | HMAC-SHA256（LINE Channel Secret） | SHA1（Meta App Secret） |
| **認證方式** | Channel Access Token（Header） | Page Access Token（Query Param） |

---

## 💻 實作計劃

### Phase 1：Instagram Webhook 接收（1-2 週）

#### 1.1 建立新的 Azure Function：`IGWebhook.cs`

```csharp
[Function("IGWebhook")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
{
    // GET: Webhook 驗證（Meta 初次設定時會呼叫）
    if (req.Method == "GET")
    {
        return VerifyWebhook(req);
    }
    
    // POST: 接收訊息
    var payload = await JsonSerializer.DeserializeAsync<IGWebhookPayload>(req.Body);
    
    // 處理訊息（與 LINE 共用核心邏輯）
    await ProcessMessageAsync(payload);
    
    return req.CreateResponse(HttpStatusCode.OK);
}
```

#### 1.2 設計統一的訊息抽象層

```csharp
public interface IMessagePlatform
{
    Task<string> GetUserMessageAsync(object payload);
    Task SendReplyAsync(string userId, string message);
    bool ValidateSignature(string signature, string body);
}

public class LineMessagePlatform : IMessagePlatform { ... }
public class InstagramMessagePlatform : IMessagePlatform { ... }
```

#### 1.3 更新 `LineWebhook.cs` 使用抽象層

將現有的 LINE 處理邏輯重構為共用服務，避免重複程式碼。

---

### Phase 2：Instagram 簽章驗證與回覆（1 週）

#### 2.1 簽章驗證（SHA1）

Instagram 使用不同的簽章演算法（SHA1 vs LINE 的 HMAC-SHA256）：

```csharp
public bool ValidateIGSignature(string signature, string body, string appSecret)
{
    var hash = SHA1.HashData(Encoding.UTF8.GetBytes(body + appSecret));
    var computedSignature = "sha1=" + BitConverter.ToString(hash).Replace("-", "").ToLower();
    return signature == computedSignature;
}
```

#### 2.2 發送回覆訊息

```csharp
public async Task SendIGReply(string recipientId, string message)
{
    var url = $"https://graph.facebook.com/v18.0/me/messages?access_token={_pageAccessToken}";
    var payload = new
    {
        recipient = new { id = recipientId },
        message = new { text = message }
    };
    
    await _httpClient.PostAsJsonAsync(url, payload);
}
```

---

### Phase 3：多租戶 Config 擴展（3-5 天）

#### 3.1 Config 新增 Instagram 設定

```json
{
  "storeName": "酷潮選貨店",
  "platforms": {
    "line": {
      "enabled": true,
      "channelId": "...",
      "webhookUrl": "https://coolshop-func.azurewebsites.net/api/linewebhook"
    },
    "instagram": {
      "enabled": true,
      "pageId": "...",
      "webhookUrl": "https://coolshop-func.azurewebsites.net/api/igwebhook"
    }
  },
  "faq": [ ... ]
}
```

#### 3.2 Key Vault 新增 Instagram Secrets

```bash
az keyvault secret set --vault-name kv{tenant}prod \
  --name Instagram-PageAccessToken --value "YOUR_PAGE_TOKEN"

az keyvault secret set --vault-name kv{tenant}prod \
  --name Instagram-AppSecret --value "YOUR_APP_SECRET"
```

---

### Phase 4：對話歷史統一管理（3-5 天）

#### 4.1 Table Storage Schema 擴展

現有的 `ConversationMessageEntity` 已經很通用，只需新增 `Platform` 欄位：

```csharp
public class ConversationMessageEntity : ITableEntity
{
    public string PartitionKey { get; set; }  // userId
    public string RowKey { get; set; }        // timestamp
    public string Platform { get; set; }      // "LINE" or "Instagram"
    public string Role { get; set; }          // "user" or "assistant"
    public string Content { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
```

#### 4.2 跨平台對話隔離

同一用戶在 LINE 與 IG 的對話應該分開儲存：

```csharp
var partitionKey = $"{platform}_{userId}";  // 例如："Instagram_1234567890"
```

---

## 🚀 部署與上線流程

### 1. Meta Business 設定

#### 1.1 建立 Meta App

1. 前往 [Meta for Developers](https://developers.facebook.com/)
2. 建立新 App → 選擇「Business」類型
3. 新增產品 → 選擇「Messenger」與「Instagram」
4. 取得 App ID 與 App Secret

#### 1.2 連結 Instagram 帳號

1. 在 App 設定中，前往「Instagram」→「基本設定」
2. 連結 Instagram 專業帳號（必須是 Business / Creator 帳號）
3. 產生 Page Access Token（永久或長期 Token）

#### 1.3 設定 Webhook

```
Callback URL: https://{tenant}-func.azurewebsites.net/api/igwebhook
Verify Token: 自訂一個隨機字串（用於初次驗證）

訂閱欄位：
✅ messages
✅ messaging_postbacks
✅ messaging_optins
```

#### 1.4 提交應用程式審核

Meta 要求 `instagram_manage_messages` 權限需要審核：

- 提供 App 使用說明與影片 Demo
- 說明商業用途（AI 客服）
- 審核時間：通常 3-7 個工作天

---

### 2. Azure 部署

```bash
# 部署包含 IG Webhook 的新版本
./scripts/deploy-app.sh {tenant}

# 上傳 Instagram Secrets
az keyvault secret set --vault-name kv{tenant}prod{random} \
  --name Instagram-PageAccessToken --value "YOUR_PAGE_TOKEN"

az keyvault secret set --vault-name kv{tenant}prod{random} \
  --name Instagram-AppSecret --value "YOUR_APP_SECRET"
```

---

### 3. 測試驗證

#### 3.1 Webhook 驗證

Meta 會發送 GET 請求驗證 Webhook：

```
GET /api/igwebhook?hub.mode=subscribe&hub.challenge=123456&hub.verify_token=YOUR_TOKEN
```

Function 需正確回傳 `hub.challenge` 參數。

#### 3.2 訊息測試

1. 在 Instagram App 中傳訊息給品牌帳號
2. 檢查 Application Insights 是否收到 Webhook 事件
3. 確認 AI 回覆正常顯示在 IG DM

---

## ⚠️ 注意事項與限制

### Meta API 限制

| 限制項目 | 說明 |
|---------|------|
| **訊息窗口（24 小時規則）** | 只能在用戶主動傳訊後 24 小時內回覆（除非使用 Message Tags） |
| **速率限制** | 每個 Page 每分鐘最多 200 次 API 呼叫 |
| **審核要求** | `instagram_manage_messages` 權限需要通過 Meta 審核 |
| **帳號類型** | 必須是 Instagram 專業帳號（Business / Creator） |

### Message Tags（延長回覆窗口）

如果需要在 24 小時後仍能回覆，可使用 Message Tags：

```json
{
  "recipient": { "id": "USER_ID" },
  "message": { "text": "..." },
  "messaging_type": "MESSAGE_TAG",
  "tag": "CONFIRMED_EVENT_UPDATE"  // 用於訂單更新等
}
```

---

## 📈 未來優化方向

### 短期（3 個月內）

- [ ] **Instagram 快速回覆（Quick Replies）**  
  提供預設選項按鈕，提升互動體驗。

- [ ] **Instagram Story Mentions**  
  監聽用戶在 Story 中 tag 品牌，自動回覆。

### 中期（6 個月內）

- [ ] **Facebook Messenger 支援**  
  同樣使用 Meta Graph API，技術架構相似。

- [ ] **統一多平台後台**  
  提供 Web Portal，店家可在單一介面管理 LINE、IG、FB 訊息。

### 長期（12 個月內）

- [ ] **WhatsApp Business API**  
  進軍海外市場必備（東南亞、歐美）。

- [ ] **全通路客服整合**  
  整合電商平台訊息中心（Shopline、91APP）。

---

## 🎓 學習資源

### 官方文件

- [Meta for Developers - Instagram Platform](https://developers.facebook.com/docs/instagram)
- [Instagram Messaging API Overview](https://developers.facebook.com/docs/messenger-platform/instagram)
- [Messenger Platform Webhooks](https://developers.facebook.com/docs/messenger-platform/webhooks)

### 教學影片

- [Meta for Developers - Getting Started](https://developers.facebook.com/docs/development/register)
- [Instagram Graph API Tutorial](https://www.youtube.com/results?search_query=instagram+graph+api+tutorial)

### 範例程式碼

- [Meta SDK for .NET](https://github.com/facebook-csharp-sdk/facebook-csharp-sdk)
- [Instagram Webhook Sample (Node.js)](https://github.com/fbsamples/messenger-platform-samples)

---

## 💡 快速開始：5 步驟驗證可行性

想快速測試 Instagram 整合？跟著這個簡化流程：

### Step 1：申請 Meta Business 帳號（10 分鐘）

前往 [Meta Business Suite](https://business.facebook.com/) 註冊。

### Step 2：建立測試 App（5 分鐘）

在 [Meta for Developers](https://developers.facebook.com/) 建立 App。

### Step 3：連結 Instagram（5 分鐘）

在 App 設定中連結你的 Instagram 帳號。

### Step 4：本地測試 Webhook（20 分鐘）

```bash
# 啟動本地 Function
func start

# 使用 ngrok 建立公開 URL
ngrok http 7071

# 在 Meta App 設定 Webhook URL（ngrok 提供的 HTTPS URL）
```

### Step 5：傳訊息測試（5 分鐘）

在 Instagram 傳訊息給品牌帳號，檢查是否收到 Webhook 事件。

---

**Buddy ShopAI — 你的全通路電商智慧好夥伴** 🤖✨
