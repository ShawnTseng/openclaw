#!/bin/bash
set -e

# Deploy app settings to Azure Function App from a local settings JSON file.
# Usage: ./deploy-settings.sh <tenant-id> [environment] [settings-file]
# environment: staging | production (default: production)
# Examples:
#   ./deploy-settings.sh mrvshop                                   # production, use local.settings.json
#   ./deploy-settings.sh mrvshop staging                           # staging
#   ./deploy-settings.sh mrvshop production ./my.settings.json    # custom file

TENANT=${1:?"Usage: $0 <tenant-id> [environment] [settings-file]"}
ENVIRONMENT=${2:-"production"}
SETTINGS_FILE=${3:-"local.settings.json"}

# Validate environment
if [[ "$ENVIRONMENT" != "staging" && "$ENVIRONMENT" != "production" ]]; then
  echo "❌ Invalid environment: $ENVIRONMENT (must be 'staging' or 'production')"
  exit 1
fi

# Derive names based on environment (matches deploy-app.sh naming convention)
if [ "$ENVIRONMENT" == "staging" ]; then
  FUNCTION_APP_NAME="${TENANT}-func-staging"
  RESOURCE_GROUP="rg-${TENANT}-staging"
else
  FUNCTION_APP_NAME="${TENANT}-func"
  RESOURCE_GROUP="rg-${TENANT}-prod"
fi

if [ ! -f "$SETTINGS_FILE" ]; then
  echo "❌ Settings file not found: $SETTINGS_FILE"
  exit 1
fi

# Verify jq is available
if ! command -v jq &>/dev/null; then
  echo "❌ 'jq' is required but not installed. Run: brew install jq"
  exit 1
fi

echo "==> [Buddy ShopAI] Deploying app settings for tenant: $TENANT"
echo "    Environment:   $ENVIRONMENT"
echo "    Function App:  $FUNCTION_APP_NAME"
echo "    Resource Group: $RESOURCE_GROUP"
echo "    Settings file: $SETTINGS_FILE"

# Keys that only apply locally and should NOT be pushed to Azure
# - AzureWebJobsStorage / FUNCTIONS_WORKER_RUNTIME: Azure 有獨立設定
# - AzureOpenAI__Endpoint / AzureOpenAI__ApiKey: 由 Bicep 自動設定，不覆蓋
#   (屬地檔往往是佔位符，会導致生產環境 DNS 失敗)
SKIP_KEYS=("AzureWebJobsStorage" "FUNCTIONS_WORKER_RUNTIME" "AzureOpenAI__Endpoint" "AzureOpenAI__ApiKey")

# Build the settings array from the JSON file
SETTINGS_ARGS=()
while IFS= read -r line; do
  key="${line%%=*}"
  value="${line#*=}"

  skip=false
  for skip_key in "${SKIP_KEYS[@]}"; do
    if [[ "$key" == "$skip_key" ]]; then
      skip=true
      break
    fi
  done

  if [ "$skip" = false ]; then
    SETTINGS_ARGS+=("${key}=${value}")
    echo "    + $key"
  else
    echo "    - $key  (skipped, local-only)"
  fi
done < <(jq -r '.Values | to_entries[] | "\(.key)=\(.value)"' "$SETTINGS_FILE")

if [ ${#SETTINGS_ARGS[@]} -eq 0 ]; then
  echo "⚠️  No settings to deploy after filtering local-only keys."
  exit 0
fi

echo ""
echo "==> Pushing ${#SETTINGS_ARGS[@]} settings to Azure..."
az functionapp config appsettings set \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --settings "${SETTINGS_ARGS[@]}" \
  --output table

echo ""
echo "==> ✅ Settings deployed successfully to $ENVIRONMENT!"
