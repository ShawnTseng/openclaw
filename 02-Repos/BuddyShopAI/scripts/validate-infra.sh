#!/bin/bash
set -e

TENANT=${1:?"Usage: $0 <tenant-id> [params-file]"}
RESOURCE_GROUP="rg-${TENANT}-prod"
PARAMS_FILE=${2:-"infra/main.parameters.${TENANT}.json"}

if [ ! -f "$PARAMS_FILE" ]; then
  echo "❌ Parameters file not found: $PARAMS_FILE"
  exit 1
fi

echo "==> [Buddy ShopAI] Validating Bicep template for tenant: $TENANT"
az deployment group validate \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters "$PARAMS_FILE"

echo "==> ✅ Validation successful for tenant: $TENANT"
