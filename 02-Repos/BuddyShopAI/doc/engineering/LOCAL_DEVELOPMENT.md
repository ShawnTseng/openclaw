# 🔧 本地開發環境設定

> 完整的本地開發環境配置指南

---

## 安裝必要工具

### macOS

```bash
# .NET 8 SDK
brew install dotnet-sdk

# Azure Functions Core Tools
brew tap azure/functions
brew install azure-functions-core-tools@4

# Azure CLI
brew install azure-cli

# Azurite (Azure Storage 本地模擬器)
npm install -g azurite

# ngrok (本地測試 LINE Webhook)
brew install ngrok/ngrok/ngrok
```

### Windows

```powershell
# 使用 Chocolatey
choco install dotnet-8.0-sdk
choco install azure-functions-core-tools
choco install azure-cli
npm install -g azurite
choco install ngrok
```

---

## 專案設定

### 1. Clone 專案

```bash
git clone https://github.com/ShawnTseng/BuddyShopAI.git
cd BuddyShopAI
```

### 2. 還原依賴

```bash
dotnet restore
```

### 3. 設定本地環境變數

```bash
cp local.settings.json.example local.settings.json
```

編輯 `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "TENANT_ID": "demo",
    "LINE__ChannelAccessToken": "YOUR_LINE_CHANNEL_ACCESS_TOKEN",
    "LINE__ChannelSecret": "YOUR_LINE_CHANNEL_SECRET",
    "AzureOpenAI__Endpoint": "https://YOUR_OPENAI_INSTANCE.openai.azure.com/",
    "AzureOpenAI__ApiKey": "YOUR_OPENAI_API_KEY",
    "AzureOpenAI__DeploymentName": "gpt-4o-mini"
  }
}
```

### 4. 建立測試租戶設定

```bash
cp configs/_template.json configs/demo.json
```

編輯 `configs/demo.json` 填入測試商店資訊。

---

## 啟動本地服務

### 1. 啟動 Azurite

```bash
# 在新的終端視窗運行
azurite --silent --location ~/.azurite --debug ~/.azurite/debug.log
```

### 2. 建置並啟動 Function App

```bash
dotnet build
func start
```

你應該會看到：

```
Azure Functions Core Tools
Core Tools Version: 4.x.x
Function Runtime Version: 4.x.x

Functions:

  LineWebhook: [POST] http://localhost:7071/api/LineWebhook
```

---

## 測試 LINE Webhook

### 方法 1: 使用 ngrok（推薦）

```bash
# 在新終端視窗
ngrok http 7071
```

複製 ngrok 提供的 HTTPS URL（如 `https://abc123.ngrok.io`），在 LINE Developers Console 設定 Webhook:

```
https://abc123.ngrok.io/api/LineWebhook
```

現在可以直接在 LINE 聊天測試！

### 方法 2: 使用 curl 測試（無需 LINE）

```bash
# 測試基本連接（應返回 401/400，表示簽章驗證正常）
curl -X POST http://localhost:7071/api/LineWebhook \
  -H "Content-Type: application/json" \
  -d '{"events":[]}'
```

---

## 開發工作流程

### 1. 修改代碼

在 VS Code 或你喜歡的編輯器中修改 `.cs` 檔案。

### 2. 熱重載

Functions Core Tools 支援熱重載，儲存檔案後會自動重新編譯：

```bash
# 監看模式（可選）
dotnet watch
```

### 3. 查看日誌

所有 `ILogger` 輸出會顯示在終端：

```
[2026-02-13 10:30:45] Executing 'LineWebhook'
[2026-02-13 10:30:45] Received message from user: U1234567890
[2026-02-13 10:30:46] AI Response: 嗨！我是 88MRV AI 客服...
```

---

## 調試 (Debugging)

### VS Code

1. 安裝 C# 擴充套件
2. 建立 `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Attach to .NET Functions",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:azureFunctions.pickProcess}"
    }
  ]
}
```

3. 啟動 `func start`
4. F5 附加調試器
5. 設定中斷點

---

## 常見問題

### Azurite 連接錯誤

```bash
# 清除 Azurite 資料
rm -rf ~/.azurite
mkdir ~/.azurite
azurite --silent --location ~/.azurite
```

### Port 7071 已被佔用

```bash
# 找出佔用的程序
lsof -i :7071

# 終止程序
kill -9 <PID>

# 或使用其他 Port
func start --port 7072
```

### LINE 簽章驗證失敗

確認 `local.settings.json` 中的 `LINE__ChannelSecret` 正確。

---

## 下一步

- 📦 [部署到 Azure](DEPLOYMENT.md)
- ⚙️ [配置管理](CONFIGURATION.md)
- 🏗️ [架構總覽](ARCHITECTURE.md)

---

**提示**: 本地開發建議使用 Azurite，避免產生 Azure Storage 費用
