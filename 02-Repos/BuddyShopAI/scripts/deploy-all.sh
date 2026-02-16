#!/bin/bash
set -e

# Buddy ShopAI - 批量部署所有租戶
# 用法: ./scripts/deploy-all.sh [infra|app|both]

ACTION=${1:-"app"}
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# 租戶列表（新增客戶時在此加入）
TENANTS=("mrvshop" "guban")

echo "============================================"
echo "  Buddy ShopAI - 批量部署"
echo "  動作: $ACTION"
echo "  租戶: ${TENANTS[*]}"
echo "============================================"

FAILED=()

for TENANT in "${TENANTS[@]}"; do
  echo ""
  echo ">>> 正在部署租戶: $TENANT"
  echo "-------------------------------------------"
  
  if [[ "$ACTION" == "infra" || "$ACTION" == "both" ]]; then
    if "$SCRIPT_DIR/deploy-infra.sh" "$TENANT"; then
      echo "✅ $TENANT 基礎設施部署成功"
    else
      echo "❌ $TENANT 基礎設施部署失敗"
      FAILED+=("$TENANT:infra")
    fi
  fi

  if [[ "$ACTION" == "app" || "$ACTION" == "both" ]]; then
    if "$SCRIPT_DIR/deploy-app.sh" "$TENANT"; then
      echo "✅ $TENANT 應用程式部署成功"
    else
      echo "❌ $TENANT 應用程式部署失敗"
      FAILED+=("$TENANT:app")
    fi
  fi
done

echo ""
echo "============================================"
if [ ${#FAILED[@]} -eq 0 ]; then
  echo "  ✅ 所有租戶部署完成！"
else
  echo "  ⚠️  以下租戶部署失敗："
  for F in "${FAILED[@]}"; do
    echo "    - $F"
  done
  exit 1
fi
echo "============================================"
