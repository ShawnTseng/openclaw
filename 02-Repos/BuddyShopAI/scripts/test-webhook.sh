#!/bin/bash

# Test LINE webhook with a simulated message event
# This requires the LINE Channel Secret to generate a valid signature

WEBHOOK_URL="https://mrvshop-func.azurewebsites.net/api/LineWebhook"
CHANNEL_SECRET="${1:-e984bc3d261bf14c510195a6a2c4ab37}"

# Sample webhook event (text message)
REQUEST_BODY='{
  "destination": "Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "events": [
    {
      "type": "message",
      "message": {
        "type": "text",
        "id": "test123",
        "text": "測試訊息"
      },
      "timestamp": 1234567890123,
      "source": {
        "type": "user",
        "userId": "Utest123456789"
      },
      "replyToken": "test-reply-token",
      "mode": "active"
    }
  ]
}'

echo "📤 Sending test webhook request..."
echo "Body: $REQUEST_BODY"
echo ""

# Generate HMAC-SHA256 signature
SIGNATURE=$(echo -n "$REQUEST_BODY" | openssl dgst -sha256 -hmac "$CHANNEL_SECRET" -binary | base64)

echo "🔐 Signature: $SIGNATURE"
echo ""

# Send request
echo "📡 Sending to: $WEBHOOK_URL"
curl -v -X POST "$WEBHOOK_URL" \
  -H "Content-Type: application/json" \
  -H "X-Line-Signature: $SIGNATURE" \
  -d "$REQUEST_BODY"

echo ""
echo "✅ Request sent. Check Application Insights for results."
