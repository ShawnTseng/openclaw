# Buddy ShopAI 開發路線圖

> 最後更新：2026-02-13

---

## 🔴 P0：上線前必做

- [ ] **LINE Webhook URL 設定（各租戶）**  
  在各租戶的 LINE Developers Console 設定 Webhook URL：  
  `https://{tenant}-func.azurewebsites.net/api/linewebhook`  
  並關閉「自動回覆訊息」、啟用 Webhook。

- [ ] **端對端測試（各租戶）**  
  用真實 LINE 帳號傳訊息，確認 AI 正常回覆。  
  驗證項目：FAQ 回答、速率限制、對話歷史、逾時重置。

- [x] **~~Key Vault Secret 驗證（mrvshop）~~** ✅  
  已確認 `kvmrvshopprodt2i` 含 3 個 Secrets：  
  AzureOpenAI-ApiKey、LINE-ChannelAccessToken、LINE-ChannelSecret  
  ```bash
  az keyvault secret list --vault-name kvmrvshopprodt2i -o table
  ```

---

## 🟡 P1：近期改善

- [x] **~~部署區域優化（降低延遲）~~** → 決策完成  
  決策：維持 East US，優先保證可用性。  
  - Japan East：gpt-4o-mini Standard 不支援 ❌  
  - Southeast Asia：支援度未知，待驗證  
  - LINE 聊天對延遲容忍度高，1-2 秒可接受  
  - 待客戶增長後再評估區域遷移 ROI  
  - 詳見 [LESSONS_LEARNED.md](LESSONS_LEARNED.md) 區域優化策略

- [ ] **Google Sheets No-Code CMS（活動資訊動態化）**  
    將「活動資訊」改為由 Google Sheets 動態讀取，`configs/{tenantId}.json` 仍維持靜態設定：
    - 店家可直接在 Google Sheet 編輯活動內容（如促銷、公告）
    - Function App 透過公開 CSV URL 定期抓取活動資訊
    - 使用 IMemoryCache（5-10 分鐘快取）避免頻繁請求
    - 降低更新門檻：活動資訊可即時調整，無需重新部署

- [ ] **store-config.json 補充更多 FAQ** → 已轉移至 `configs/{tenantId}.json`  
  根據各租戶實際營運情況，擴充常見問答：
  - 尺寸選擇建議（身高體重對照）
  - 付款方式
  - 預購流程
  - 品牌介紹

- [ ] **冷啟動優化**  
  Consumption Plan 閒置後冷啟動可能 5-15 秒。  
  方案：
  - 使用免費的 UptimeRobot 每 5 分鐘 Ping Webhook URL
  - 或接受冷啟動延遲（LINE 用戶不太敏感）

---

## 🟢 P2：功能擴展

- [ ] **新店推銷自動化流程**  
  建立標準化的新店推銷流程與工具：
  - 網路資訊自動搜集工具（從 IG、FB、官網等抓取）
  - config 自動產生器（將搜集的資訊轉為 config 格式）
  - 推銷範例 config 與展示文檔
  - 詳見 [ONBOARDING.md](../business/ONBOARDING.md)

- [ ] **Instagram 平台支援 (Multi-Platform)**  
  擴展至 Instagram Direct Message：
  - 研究 Instagram Messaging API（需 Meta Business）
  - 設計統一的訊息抽象層（支援 LINE/IG 雙平台）
  - Webhook 路由機制（根據平台分流）
  - 詳見 [INSTAGRAM_INTEGRATION.md](../features/INSTAGRAM_INTEGRATION.md)

- [ ] **Rich Menu 與 Flex Message**  
  提供更豐富的 LINE 互動體驗：
  - Rich Menu：底部選單快捷鍵（查運費、查訂單、聯絡客服）
  - Flex Message：結構化卡片回覆（商品推薦、訂單狀態）

- [ ] **以圖搜圖 / 視覺辨識 (Phase 2)**  
  利用 GPT-4o Vision：
  - 客人傳截圖 → AI 辨識商品 → 提供連結
  - 需升級模型到 gpt-4o（成本較高）

- [ ] **PDF 知識庫 RAG (Phase 2)**  
  讓業主上傳品牌手冊、布料保養指南：
  - 使用 Embedding + Vector Search
  - 可考慮 Azure AI Search

- [ ] **系統串接 (Phase 3)**  
  串接電商平台 API：
  - Shopline / Cyberbiz / POS
  - 會員查詢、訂單追蹤

---

## 🔧 技術債務

- [x] ~~**刪除根目錄 DEPLOYMENT.md**~~  
  已搬移至 `doc/deployment/DEPLOYMENT_GUIDE.md`，根目錄舊檔案已刪除。

- [x] ~~**清理過時的 Bicep 參數檔**~~  
  已刪除 `main.parameters.json`、`main.parameters.prod.json`、`main.parameters.prod.json.example`  
  現用 `main.parameters.template.json`（範本）+ `main.parameters.{tenantId}.json`（各租戶）

- [x] ~~**deploy-infra.sh 預設 location**~~  
  已更新：`scripts/deploy-infra.sh` 預設 location 改為 `eastus`。

- [ ] **In-Memory 狀態的限制**  
  訊息防抖和速率限制使用 In-Memory Dictionary，Consumption Plan instance 回收後重置。  
  目前可接受（速率限制重置 = 對用戶更寬鬆），但若需嚴格限制，可改用 Table Storage 或 Redis。

- [ ] **單元測試**  
  目前無任何測試。優先考慮：
  - `LineSignatureValidator` 簽章驗證
  - `ConversationHistoryService` 防抖邏輯
  - `PromptProvider` 知識庫載入

- [ ] **Prompt 工程優化**  
  根據實際用戶對話數據，持續調優 System Prompt：
  - 回答長度控制
  - Emoji 使用頻率
  - 轉人工客服的判斷閾值

---

## ✅ 已完成

- [x] Azure OpenAI 整合（取代 Google Gemini）
- [x] Azure Table Storage 對話歷史（取代 IMemoryCache）
- [x] Key Vault + Managed Identity（密鑰安全管理）
- [x] 對話逾時 (24h) 與速率限制 (10問/時)
- [x] 訊息防抖 (3s debounce)
- [x] Consumption Plan 部署（繞過 VM 配額限制）
- [x] 資源清理（刪除所有不必要資源）
- [x] Bicep IaC 簡化（4 modules）
- [x] Webhook 部署驗證（POST → 401）
- [x] 文件知識庫重整
- [x] 多租戶平台化改造（MrvShopAI → BuddyShopAI）
- [x] mrvshop 首次部署（infra + app，East US）
- [x] Key Vault RBAC 角色設定
- [x] 部署區域決策（East US，優先可用性）
- [x] README.md 全面改版（Buddy ShopAI 品牌）
