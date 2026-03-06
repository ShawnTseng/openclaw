# Buddy ShopAI 文檔索引 📚

> 文檔導航中心 — 快速找到你需要的資訊

---

## 📂 文檔結構

```
doc/
├── README.md                          # 📍 你在這裡
├── engineering/                        # 🔧 工程技術文檔（開發 + 維運）
│   ├── QUICKSTART.md                  # 快速開始
│   ├── LOCAL_DEVELOPMENT.md           # 本地開發環境
│   ├── ARCHITECTURE.md                # 系統架構總覽
│   ├── SECURITY.md                    # 安全架構
│   ├── FEATURES.md                    # ★ 所有功能總覽
│   ├── DEPLOYMENT.md                  # 部署指南
│   ├── CONFIGURATION.md               # 配置管理
│   ├── MONITORING.md                  # 監控與維運
│   ├── TROUBLESHOOTING.md             # 故障排除
│   ├── COST_OPTIMIZATION.md           # 成本優化
│   ├── CHANGELOG.md                   # 變更歷史
│   ├── ROADMAP.md                     # 開發路線圖
│   ├── LESSONS_LEARNED.md             # 經驗教訓與技術決策
│   ├── NAMING_CONVENTIONS.md          # 命名規範
│   ├── INSTAGRAM_INTEGRATION.md       # Instagram 整合計劃
│   └── manage-api.http                # 管理 API 測試檔
└── business/                           # 💼 商業文檔
    ├── BUSINESS_MODEL.md              # 商業模式
    ├── PRICING.md                     # 定價策略
    ├── ONBOARDING.md                  # 客戶上線流程
    ├── QUOTATION_AND_SCOPE.md         # 報價暨服務範圍
    └── CHECKLIST_20260221.md          # 客戶拜訪準備清單
```

---

## 🔍 快速查找

**我想...**

| 需求 | 文件 |
|------|------|
| 🆕 快速試用 | [QUICKSTART.md](engineering/QUICKSTART.md) |
| 💻 設定開發環境 | [LOCAL_DEVELOPMENT.md](engineering/LOCAL_DEVELOPMENT.md) |
| ☁️ 部署到 Azure | [DEPLOYMENT.md](engineering/DEPLOYMENT.md) |
| ✨ 了解所有功能 | [FEATURES.md](engineering/FEATURES.md) |
| 🏗️ 了解系統架構 | [ARCHITECTURE.md](engineering/ARCHITECTURE.md) |
| ⚙️ 設定新租戶 | [CONFIGURATION.md](engineering/CONFIGURATION.md) |
| 📊 監控與除錯 | [MONITORING.md](engineering/MONITORING.md) |
| 🐛 解決問題 | [TROUBLESHOOTING.md](engineering/TROUBLESHOOTING.md) |
| 💰 了解成本 | [COST_OPTIMIZATION.md](engineering/COST_OPTIMIZATION.md) |
| 🔐 了解安全機制 | [SECURITY.md](engineering/SECURITY.md) |
| 📱 Instagram 整合 | [INSTAGRAM_INTEGRATION.md](engineering/INSTAGRAM_INTEGRATION.md) |
| 🎯 了解商業模式 | [BUSINESS_MODEL.md](business/BUSINESS_MODEL.md) |
| 💲 了解定價 | [PRICING.md](business/PRICING.md) |
| 👥 新增客戶 SOP | [ONBOARDING.md](business/ONBOARDING.md) |

---

## 🔧 GitHub Actions 工作流程

| Workflow | 觸發方式 | 說明 |
|----------|---------|------|
| `deploy.yml` | dev push / 手動 | 部署應用到 staging 或 production |
| `provision.yml` | 手動（按鈕） | 佈建 Azure 基礎設施（一鍵建環境） |
| `promote.yml` | 手動（按鈕） | Staging → Production 模擬 slot swap |
| `keep-warm.yml` | 每 4 分鐘 / 手動 | 防冷啟動，ping Staging health 端點 |

---

**維護者**: Shawn Tseng  
**最後更新**: 2026-02-22
