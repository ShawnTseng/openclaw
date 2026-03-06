#!/bin/bash
set -e

# Usage: ./deploy-infra.sh <tenant-id> [environment] [location] [params-file]
# environment: staging | production (default: production)
# Examples:
#   ./deploy-infra.sh mrvshop                          # production
#   ./deploy-infra.sh mrvshop staging                  # staging
#   ./deploy-infra.sh mrvshop production eastasia     # production with location

TENANT=${1:?"Usage: $0 <tenant-id> [environment] [location] [params-file]"}
ENVIRONMENT=${2:-"production"}
LOCATION=${3:-"eastasia"}
PARAMS_FILE=${4:-"infra/main.parameters.${TENANT}.json"}

# Validate environment
if [[ "$ENVIRONMENT" != "staging" && "$ENVIRONMENT" != "production" ]]; then
  echo "❌ Invalid environment: $ENVIRONMENT (must be 'staging' or 'production')"
  exit 1
fi

# Derive resource group name based on environment
if [ "$ENVIRONMENT" == "staging" ]; then
  RESOURCE_GROUP="rg-${TENANT}-staging"
else
  RESOURCE_GROUP="rg-${TENANT}-prod"
fi

if [ ! -f "$PARAMS_FILE" ]; then
  echo "❌ Parameters file not found: $PARAMS_FILE"
  echo "   Please create it from infra/main.parameters.template.json"
  exit 1
fi

echo "==> [Buddy ShopAI] Deploying infrastructure for tenant: $TENANT"
echo "    Environment: $ENVIRONMENT"
echo "    Resource Group: $RESOURCE_GROUP"
echo "    Location: $LOCATION"
echo "    Parameters: $PARAMS_FILE"

echo "==> Creating resource group: $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION"

echo "==> Deploying infrastructure..."
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters "$PARAMS_FILE" \
  --parameters environment="$ENVIRONMENT"

echo "==> ✅ Deployment complete for tenant: $TENANT ($ENVIRONMENT)"
LATEST=$(az deployment group list \
  --resource-group "$RESOURCE_GROUP" \
  --query "[0].name" -o tsv)
az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$LATEST" \
  --query properties.outputs
