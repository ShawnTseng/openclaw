#!/bin/bash
# ======================================================
# BuddyShopAI QA 煙霧測試腳本
#
# 用途：對全部 24 個 QA 情境送出 LINE Webhook 請求，
#       驗證每個端點皆回傳 HTTP 200（服務正常可接收）。
#
# ⚠️  注意：本腳本只驗證「可達性」（HTTP 200），
#       AI 回覆品質的驗收以 doc/mrvshop-qa.md 為準。
#
# 本機執行：
#   export LINE_CHANNEL_SECRET="your-secret"
#   export WEBHOOK_URL="https://mrvshop-func-staging.azurewebsites.net/api/LineWebhook"
#   bash scripts/qa-smoke.sh
#
# GitHub Actions：
#   由 .github/workflows/qa.yml 自動呼叫，
#   LINE_CHANNEL_SECRET 從 GitHub Secrets 傳入。
#
# 一致性：情境 ID（Q01–Q24）與以下文件同步：
#   - doc/mrvshop-qa.md         （行為定義 / 驗收標準）
#   - doc/engineering/qa-scenarios.http  （手動測試）
# ======================================================

set -eo pipefail

WEBHOOK_URL="${WEBHOOK_URL:-https://mrvshop-func-staging.azurewebsites.net/api/LineWebhook}"
CHANNEL_SECRET="${LINE_CHANNEL_SECRET:?Error: LINE_CHANNEL_SECRET is not set}"
TEST_USER="${TEST_USER_ID:-Uqa_smoke_test_user}"
TIMESTAMP=$(date +%s000)

PASS=0
FAIL=0
declare -a ERRORS

# ── Helper: 送出請求並驗證 HTTP 200 ──────────────────────────────────────────
send_and_verify() {
  local case_id="$1"
  local description="$2"
  local message_text="$3"

  # Compact JSON（避免換行影響 HMAC 計算）
  local BODY
  BODY=$(printf '{"destination":"Utest","events":[{"type":"message","message":{"type":"text","id":"qa-%s","text":"%s"},"webhookEventId":"qa-%s","deliveryContext":{"isRedelivery":false},"timestamp":%s,"source":{"type":"user","userId":"%s"},"replyToken":"qa-reply-%s","mode":"active"}]}' \
    "$case_id" "$message_text" "$case_id" "$TIMESTAMP" "$TEST_USER" "$case_id")

  local SIG
  SIG=$(printf '%s' "$BODY" | openssl dgst -sha256 -hmac "$CHANNEL_SECRET" -binary | base64)

  local STATUS
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$WEBHOOK_URL" \
    -H "Content-Type: application/json" \
    -H "X-Line-Signature: $SIG" \
    -d "$BODY" \
    --max-time 15)

  if [ "$STATUS" = "200" ]; then
    echo "  ✅ [$case_id] $description"
    PASS=$((PASS + 1))
  else
    echo "  ❌ [$case_id] $description  →  HTTP $STATUS"
    FAIL=$((FAIL + 1))
    ERRORS+=("[$case_id] $description → HTTP $STATUS")
  fi
}

# ─────────────────────────────────────────────────────────────────────────────

echo ""
echo "🤖  BuddyShopAI QA 煙霧測試"
echo "📍  目標：$WEBHOOK_URL"
echo "──────────────────────────────────────────────────────"

echo ""
echo "01. 庫存與現貨"
send_and_verify "Q01" "詢問有無現貨" "有現貨嗎"
send_and_verify "Q02" "詢問是否隨時有貨" "賣完會補貨嗎"

echo ""
echo "02. 尺寸相關"
send_and_verify "Q03" "詢問身高體重推薦尺寸" "身高170體重60穿什麼尺寸"
send_and_verify "Q04" "詢問鞋款尺寸換算" "鞋號怎麼換算"

echo ""
echo "03. 贈品"
send_and_verify "Q05" "詢問贈品如何選" "贈品怎麼選"

echo ""
echo "04. 物流與到貨"
send_and_verify "Q06" "查詢特定訂單物流進度" "我的貨到哪裡了"
send_and_verify "Q07" "一般到貨天數詢問" "下單後幾天到貨"
send_and_verify "Q08" "要求到貨通知" "到的時候跟我說"

echo ""
echo "05. 斷貨改款"
send_and_verify "Q09" "斷貨改款 - 需改款通知" "收到通知商品沒了要改款"
send_and_verify "Q10" "改款是否選相同價位" "改款要選一樣價錢的嗎"
send_and_verify "Q11" "改款是否補足件數" "一定要挑斷貨的件數嗎"
send_and_verify "Q12" "改款後物流（二次補寄）" "改款的商品會一起寄還是分開寄"
send_and_verify "Q13" "斷貨不想改款（第一階段）" "我不想改款了"

echo ""
echo "06. 售後服務"
send_and_verify "Q14" "收到商品有瑕疵" "收到商品有瑕疵"
send_and_verify "Q15" "退換貨詢問" "可以退貨嗎"
send_and_verify "Q16" "換貨流程（第一階段）" "我要換貨"
send_and_verify "Q17" "訂單未寄出想換貨" "東西還沒寄出能換嗎"
send_and_verify "Q18" "未收到完整商品（少件）" "少件"

echo ""
echo "07. 付款與訂金"
send_and_verify "Q19" "訂金繳費流程（第一階段）" "訂金怎麼繳"
send_and_verify "Q20" "訂金是否扣抵尾款" "訂金會扣掉嗎"

echo ""
echo "08. 其他情境"
send_and_verify "Q21" "商品選項異常 - 轉接" "顏色選項消失了"
send_and_verify "Q22" "客人情緒激動 - 轉接" "你們這家店太爛了，我要投訴"
send_and_verify "Q23" "個資未說明需求" "王小明 3/1 下單 test@gmail.com"
send_and_verify "Q24" "詢問簡訊是否為店家發出 - 轉接" "請問這是你們的簡訊嗎"

# ── 結果摘要 ─────────────────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  結果：$PASS 通過 ／ $FAIL 失敗  （共 $((PASS + FAIL)) 項）"
echo "══════════════════════════════════════════════════════"

if [ ${#ERRORS[@]} -gt 0 ]; then
  echo ""
  echo "失敗項目："
  for err in "${ERRORS[@]}"; do
    echo "  ✗ $err"
  done
  echo ""
  exit 1
fi

echo ""
echo "✅  全部通過！回覆品質驗收請對照 doc/mrvshop-qa.md"
echo ""
