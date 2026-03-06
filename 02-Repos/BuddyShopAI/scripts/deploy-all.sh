#!/bin/bash
set -e

# Buddy ShopAI - 批量部署所有租戶
# 用法: ./scripts/deploy-all.sh [infra|app|both] [staging|production]

ACTION=${1:-"app"}
ENVIRONMENT=${2:-"production"}
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# 租戶列表（新增客戶時在此加入）
# 例：TENANTS=("mrvshop" "guban")
TENANTS=("mrvshop")

echo "============================================"
echo "  Buddy ShopAI - 批量部署"
echo "  動作: $ACTION"
echo "  環境: $ENVIRONMENT"
echo "  租戶: ${TENANTS[*]}"
echo "============================================"

FAILED=()

for TENANT in "${TENANTS[@]}"; do
  echo ""
  echo ">>> 正在部署租戶: $TENANT ($ENVIRONMENT)"
  echo "-------------------------------------------"
  
  if [[ "$ACTION" == "infra" || "$ACTION" == "both" ]]; then
    if "$SCRIPT_DIR/deploy-infra.sh" "$TENANT" "$ENVIRONMENT"; then
      echo "✅ $TENANT 基礎設施部署成功 ($ENVIRONMENT)"
    else
      echo "❌ $TENANT 基礎設施部署失敗 ($ENVIRONMENT)"
      FAILED+=("$TENANT:infra:$ENVIRONMENT")
    fi
  fi

  if [[ "$ACTION" == "app" || "$ACTION" == "both" ]]; then
    if "$SCRIPT_DIR/deploy-app.sh" "$TENANT" "$ENVIRONMENT"; then
      echo "✅ $TENANT 應用程式部署成功 ($ENVIRONMENT)"
    else
      echo "❌ $TENANT 應用程式部署失敗 ($ENVIRONMENT)"
      FAILED+=("$TENANT:app:$ENVIRONMENT")
    fi
  fi
done

echo ""
echo "============================================"
if [ ${#FAILED[@]} -eq 0 ]; then
  echo "  ✅ 所有租戶部署完成！($ENVIRONMENT)"
else
  echo "  ⚠️  以下租戶部署失敗："
  for F in "${FAILED[@]}"; do
    echo "    - $F"
  done
  exit 1
fi
echo "============================================"
