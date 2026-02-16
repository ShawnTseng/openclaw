#!/bin/bash
set -e

TENANT=${1:?"Usage: $0 <tenant-id> [location] [params-file]"}
RESOURCE_GROUP="rg-${TENANT}-prod"
LOCATION=${2:-"eastus"}
PARAMS_FILE=${3:-"infra/main.parameters.${TENANT}.json"}

if [ ! -f "$PARAMS_FILE" ]; then
  echo "❌ Parameters file not found: $PARAMS_FILE"
  echo "   Please create it from infra/main.parameters.template.json"
  exit 1
fi

echo "==> [Buddy ShopAI] Deploying infrastructure for tenant: $TENANT"
echo "    Resource Group: $RESOURCE_GROUP"
echo "    Location: $LOCATION"
echo "    Parameters: $PARAMS_FILE"

echo "==> Creating resource group: $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION"

echo "==> Deploying infrastructure..."
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters "$PARAMS_FILE"

echo "==> ✅ Deployment complete for tenant: $TENANT"
az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name main \
  --query properties.outputs
