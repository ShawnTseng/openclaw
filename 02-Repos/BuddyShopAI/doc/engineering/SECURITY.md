# 🔐 安全架構

> Buddy ShopAI 安全機制與最佳實踐

---

## 安全設計原則

1. **零信任架構** - 所有請求都需驗證
2. **最小權限原則** - 僅授予必要的存取權限
3. **深度防禦** - 多層安全控制
4. **密鑰隔離** - 密鑰永不明文儲存
5. **審計追蹤** - 所有操作都可追溯

---

## 密鑰管理

### Azure Key Vault

所有敏感資訊儲存於 Key Vault：

| Secret 名稱 | 用途 |
|------------|------|
| `LINE-ChannelAccessToken` | LINE Messaging API 認證 |
| `LINE-ChannelSecret` | LINE Webhook 簽章驗證 |
| `AzureOpenAI-ApiKey` | Azure OpenAI API 認證 |

### Managed Identity

- Function App 使用 **System-Assigned Managed Identity**
- 無需管理密碼或 credential rotation
- 透過 Azure AD 自動認證

### RBAC 權限

```bash
# Function App 僅需 Secrets User 權限
az role assignment create \
  --assignee <MANAGED_IDENTITY_PRINCIPAL_ID> \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/.../vaults/kv{tenant}prod
```

### Key Vault References

App Settings 使用 Key Vault References：

```json
{
  "LINE__ChannelAccessToken": "@Microsoft.KeyVault(SecretUri=https://kvmrvshop.vault.azure.net/secrets/LINE-ChannelAccessToken/)",
  "LINE__ChannelSecret": "@Microsoft.KeyVault(SecretUri=https://kvmrvshop.vault.azure.net/secrets/LINE-ChannelSecret/)"
}
```

---

## Webhook 安全

### 簽章驗證 (HMAC-SHA256)

所有 LINE Webhook 請求都需驗證簽章：

```csharp
public bool ValidateSignature(string signature, string body, string channelSecret)
{
    var key = Encoding.UTF8.GetBytes(channelSecret);
    using var hmac = new HMACSHA256(key);
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
    var computedSignature = Convert.ToBase64String(hash);
    
    return signature == computedSignature;
}
```

### 請求來源驗證

- 只接受來自 LINE Platform 的請求
- 檢查 `X-Line-Signature` Header
- 無效簽章直接返回 401 Unauthorized

---

## 資料保護

### 傳輸層安全

- ✅ 強制 HTTPS (`httpsOnly: true`)
- ✅ 最低 TLS 1.2 (`minTlsVersion: '1.2'`)
- ✅ 停用 FTPS (`ftpsState: 'Disabled'`)

### 儲存層安全

- Azure Storage 使用 encryption at rest（預設啟用）
- Table Storage 僅儲存對話內容（無個資）
- 資料保留策略：24小時無活動自動清除

### 資料隔離

- 每租戶獨立 Storage Account
- PartitionKey = userId 確保用戶資料隔離
- 無跨租戶資料存取

---

## 存取控制

### Function App 認證

- AuthorizationLevel: `Function`
- 需要 Function Key 才能呼叫（LINE Platform 除外）
- Webhook URL 包含唯一的 code 參數

### 網路安全

- 目前：公開 HTTPS endpoint（LINE Webhook 需求）
- 未來可選：
  - VNet Integration（Premium Plan）
  - Private Endpoint（Premium Plan）

---

## 監控與審計

### Application Insights

自動收集：

- 所有 HTTP 請求與回應
- Exception 與 Error traces
- Dependencies (OpenAI, Table Storage)
- Custom Events

### 日誌策略

```csharp
// 不記錄敏感資訊
_logger.LogInformation("User {UserId} sent message", userId);  // ✅ Good
_logger.LogInformation("Message: {Content}", message);         // ❌ Bad (可能含個資)
```

### 告警設定（規劃中）

- API Key 即將過期
- 異常流量模式
- 錯誤率超過閾值
- 成本超過預算

---

## 安全檢查清單

### 部署前

- [ ] Key Vault 已建立並設定 Secrets
- [ ] Managed Identity 已啟用
- [ ] RBAC 角色已正確授予
- [ ] App Settings 使用 Key Vault References
- [ ] HTTPS 強制啟用
- [ ] TLS 版本設為 1.2+

### 部署後

- [ ] Webhook 簽章驗證正常（測試返回 401）
- [ ] Function App 可正常存取 Key Vault
- [ ] 日誌沒有密鑰洩漏
- [ ] LINE Webhook URL 設定正確

### 定期檢查

- [ ] 每季檢查 RBAC 權限（最小權限）
- [ ] 每半年更新依賴套件
- [ ] 監控 Azure Security Center 建議

---

## 安全事件回應

### 發現密鑰洩漏

1. 立即在 LINE Developers Console 重新產生 Channel Access Token
2. 在 Azure Portal 更新 Key Vault Secret
3. 重啟 Function App 載入新密鑰
4. 檢查日誌確認沒有異常使用

### 發現異常流量

1. 檢查 Application Insights 確認來源
2. 如確認為攻擊，暫時停用 Function App
3. 分析攻擊模式並加強防禦
4. 恢復服務並持續監控

---

## 合規性

### 個資保護

- 不儲存用戶姓名、電話、地址等個資
- 僅儲存 LINE User ID（由 LINE 提供的匿名 ID）
- 對話內容保留 24 小時後自動清除

### GDPR（若適用）

- 用戶可要求刪除所有對話記錄
- 實作方式：刪除 Table Storage 中對應的 Partition

---

## 最佳實踐

✅ **DO**
- 使用 Managed Identity
- 所有密鑰存放 Key Vault
- 啟用 HTTPS 與 TLS 1.2+
- 定期更新依賴套件
- 記錄審計日誌

❌ **DON'T**
- 將密鑰寫入代碼或 Git
- 在日誌中記錄敏感資訊
- 給予過度的 RBAC 權限
- 使用 HTTP 或 TLS 1.0/1.1
- 忽略 Security Center 建議

---

## 參考資料

- [Azure Key Vault Best Practices](https://learn.microsoft.com/azure/key-vault/general/best-practices)
- [Managed Identities for Azure Resources](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [Azure Functions Security](https://learn.microsoft.com/azure/azure-functions/security-concepts)
- [LINE Messaging API Security](https://developers.line.biz/en/docs/messaging-api/receiving-messages/#verifying-signatures)

---

**安全負責人**: Shawn Tseng  
**最後更新**: 2026-02-13
