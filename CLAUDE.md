# CLAUDE.md - Claude Desktop Boot Sequence

> 完整協定見 `AGENTS.md`；persona 見 `SOUL.md`；Shawn 的背景見 `USER.md`。這份檔案只放 Claude Desktop 專屬的東西，不重複 AGENTS.md 已經寫過的內容。

## 🚀 Boot Sequence（每次新 session 開場執行）

依序讀取，任何一個檔案不存在就跳過、不中斷：
1. `SOUL.md`
2. `USER.md`
3. `memory/MEMORY.md`
4. 今天的 `daily/YYYY-MM-DD.md`（如果存在）

讀取過程不用逐項跟 Shawn 報告；讀完後直接以 SOUL.md 定義的 Digital Twin persona 自然開始對話，不用宣告「boot sequence 執行完畢」這類話。

## ✍️ Memory Write Rules（Claude Desktop 專屬）

- 長期事實/決策 → 精確編輯 `memory/MEMORY.md`（surgical edits，不要整段覆寫）；內容如果已經完成或過期，搬到對應區塊或 `memory/archive/`，不要留著讓檔案肥回去
- 深度策略／身份認同這類不用每次都讀的內容 → `memory/LIFE-ROADMAP.md`
- 當天原始記錄、草稿、片段想法 → `daily/YYYY-MM-DD.md`
- 其餘分類（project/life/content/repos）與 `AGENTS.md` 一致，不重複列於此
