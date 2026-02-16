# 🎯 新店推銷與自動化上線流程

> Buddy ShopAI 新客戶導入標準作業程序  
> 最後更新：2026-02-13

---

## 📋 流程總覽

新店推銷的完整流程分為 5 個階段：

```
1️⃣ 前期調研      2️⃣ 資訊搜集      3️⃣ Config 產生    4️⃣ 技術部署      5️⃣ 上線驗證
   (1-2 天)         (1-2 天)         (30 分鐘)         (1-2 小時)       (1 天)
```

---

## 1️⃣ 前期調研（潛在客戶篩選）

### 目標客群特徵

| 條件 | 說明 |
|------|------|
| 產業 | 服飾電商（服裝、配件、鞋包） |
| 規模 | 月營收 50 萬 - 500 萬的中小型店家 |
| 平台 | 已有 LINE 官方帳號（或願意申請） |
| 痛點 | 客服回覆量大、FAQ 重複、夜間無人值班 |
| 預算 | 能接受月費 $4,500 起的 SaaS 服務 |

### 調研資訊清單

- [ ] 店名與品牌定位
- [ ] 主要銷售品項（類別、風格、價格帶）
- [ ] 現有客服管道（LINE、IG、FB、電話）
- [ ] 所有線上連結（官網、IG、FB、LINE、其他平台）

---

## 2️⃣ 資訊搜集（上線問卷）

> 請店家提供所有線上的連結（如：官網、IG、FB、LINE、其他平台），我們將根據這些資訊協助完成上線設定。

---

## 3️⃣ Config 產生（自動化腳本）

#### 互動式問答流程

```
🎯 Buddy ShopAI Config Generator

請輸入租戶 ID（英文小寫，如 mrvshop）：coolshop
請輸入店名：酷潮選貨店
請輸入營業時間：每日 11:00 - 22:00
請輸入 Instagram 帳號：@cool_shop_tw
...（依序填寫）...

✅ Config 檔案已產生：/configs/coolshop.json
```

#### 手動編輯範本

如果不使用腳本，也可以直接複製 [`configs/_template.json`](../configs/_template.json) 並手動修改：

```bash
cp configs/_template.json configs/newshop.json
# 編輯 newshop.json
```

---

## 4️⃣ 技術部署（自動化腳本）

### 前置需求

- [ ] Azure 訂閱帳號
- [ ] LINE Developers 帳號（已申請 Messaging API Channel）
- [ ] 已安裝 Azure CLI、.NET 8 SDK、Azure Functions Core Tools

### 部署步驟

```bash
# 1. 設定租戶 ID 與 Azure Location
export TENANT_ID=newshop
export LOCATION=eastus

# 2. 一鍵部署基礎設施 + 應用程式
./scripts/deploy-all.sh $TENANT_ID

# 3. 設定 LINE Webhook（在 LINE Developers Console）
# Webhook URL: https://newshop-func.azurewebsites.net/api/linewebhook
# 啟用 Webhook、關閉自動回覆訊息

# 4. 上傳 Secrets 至 Key Vault
az keyvault secret set --vault-name kv${TENANT_ID}prod* \
  --name LINE-ChannelAccessToken --value "YOUR_ACCESS_TOKEN"

az keyvault secret set --vault-name kv${TENANT_ID}prod* \
  --name LINE-ChannelSecret --value "YOUR_CHANNEL_SECRET"

az keyvault secret set --vault-name kv${TENANT_ID}prod* \
  --name AzureOpenAI-ApiKey --value "YOUR_OPENAI_KEY"
```

詳細部署說明請見 [部署指南](../deployment/DEPLOYMENT_GUIDE.md)。

---

## 5️⃣ 上線驗證（功能測試）

### 測試清單

| # | 測試項目 | 預期結果 | 實際結果 |
|---|---------|---------|---------|
| 1 | LINE 掃描 QR Code 加入好友 | 自動回覆歡迎訊息 | ✅ / ❌ |
| 2 | 傳送「運費」 | 回覆正確的運費政策 | ✅ / ❌ |
| 3 | 傳送「退貨」 | 回覆正確的退換貨規則 | ✅ / ❌ |
| 4 | 傳送「營業時間」 | 回覆正確的營業時間 | ✅ / ❌ |
| 5 | 傳送隨機問題（如「有什麼新品」） | AI 自然回答或引導聯絡客服 | ✅ / ❌ |
| 6 | 短時間內傳送 3 則訊息 | 訊息防抖（合併回覆） | ✅ / ❌ |
| 7 | 1 小時內傳送 10 則訊息 | 觸發速率限制提示 | ✅ / ❌ |
| 8 | Application Insights 查看 Log | 可看到請求記錄與遙測 | ✅ / ❌ |

---

## 🎁 推銷範例：完整 Demo 腳本

### Demo 情境：「酷潮選貨店」

**背景**：一家專賣韓系街頭風格的服飾店，IG 粉絲 8,000，LINE 好友 1,200 人，每月客服訊息約 300 則。

#### Demo 流程（10 分鐘）

1. **展示現有問題（痛點）**  
   「你們店現在是怎麼回覆客人的？人工回覆嗎？」  
   ➔ 展示人工回覆的時間成本與重複性問題。

2. **展示 Buddy ShopAI 解決方案**  
   「我們來看看 AI 客服如何自動回答常見問題。」  
   ➔ 打開 LINE，傳送「運費」、「退貨」、「營業時間」，AI 立即回覆。

3. **展示智慧對話能力**  
   「如果客人問『有沒有適合 170cm 的牛仔褲』呢？」  
   ➔ AI 會根據你的品牌風格與商品資訊智慧回答。

4. **展示後台設定彈性**  
   「活動資訊可以隨時更新，不用寫程式。」  
   ➔ 展示 `configs/coolshop.json` 編輯介面（未來可用 Google Sheets）。

5. **報價與合約**  
   「基本方案月費 $4,500，包含無限對話、24/7 運作、Azure 企業級穩定性。」  
   ➔ 提供報價單與試用期（首月 5 折）。

---

## 📊 推銷話術範本

### 開場白

> 「嗨！我是 Shawn，開發了 Buddy ShopAI — 專為服飾電商設計的 LINE AI 客服。  
> 我們已經幫助 **88MRV** 這類的服飾店，節省了 70% 的客服時間，讓老闆專心做生意。  
> 想了解 AI 客服怎麼幫你的店自動回覆客人嗎？」

### 痛點挖掘

- 「你們現在平均一天要回幾則客服訊息？」
- 「有沒有發現很多問題都重複（運費、尺寸、退貨）？」
- 「晚上 10 點後還有客人傳訊息，但你們已經下班了，怎麼辦？」

### 價值主張

- **省時間**：AI 自動回覆 80% 的 FAQ，客服只處理複雜問題
- **不漏單**：24/7 全天候回應，不錯過任何潛在客戶
- **提升體驗**：客人問問題立即得到回覆，滿意度提升
- **低成本**：月費 $4,500 起，比請一個兼職客服還便宜

### 異議處理

| 客戶疑慮 | 回應話術 |
|---------|---------|
| 「AI 會不會回答錯誤？」 | 「我們已經訓練好 FAQ，你也可以隨時調整。不會的問題，AI 會引導客人聯絡真人客服。」 |
| 「設定很複雜嗎？」 | 「完全不用寫程式！我們幫你搞定，你只要提供店家資訊（運費、退貨政策等），10 分鐘就能上線。」 |
| 「如果客人要查訂單怎麼辦？」 | 「目前 AI 會引導客人提供訂單編號，然後轉給真人處理。未來可以串接你的電商系統自動查詢。」 |
| 「月費太貴了。」 | 「我們算一下：一個兼職客服月薪至少 $15,000，而 Buddy ShopAI 只要 $4,500，還能 24/7 不休息。首月還有 5 折優惠！」 |

---

## 🚀 未來自動化規劃

### 短期（3 個月內）

- [ ] **Config Generator Web UI**  
  提供網頁介面，店家直接填表單即可產生 config。

- [ ] **Google Sheets No-Code CMS**  
  店家直接在 Google Sheet 編輯活動資訊，無需重新部署。

- [ ] **一鍵部署 Web Portal**  
  提供 Web 界面，店家自助註冊、部署、管理。

### 中期（6 個月內）

- [ ] **Instagram 平台支援**  
  支援 Instagram Direct Message 自動回覆。

- [ ] **電商平台串接**  
  自動查詢訂單狀態（Shopline / Cyberbiz）。

- [ ] **以圖搜圖功能**  
  客人傳截圖，AI 自動辨識商品並推薦。

---

## 📞 聯絡方式

有任何問題或需要協助，請聯絡：

- Email: shawn@buddyshopai.com
- LINE: @buddyshopai
- Phone: 0912-345-678

---

**Buddy ShopAI — 你的電商服飾智慧好夥伴** 🤖✨
