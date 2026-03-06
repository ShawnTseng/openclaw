#!/bin/bash
set -e

# Usage: ./deploy-app.sh <tenant-id> [environment] [function-app-name] [resource-group]
# environment: staging | production (default: production)
# Examples:
#   ./deploy-app.sh mrvshop                    # production (auto-derived names)
#   ./deploy-app.sh mrvshop staging            # staging (auto-derived names)
#   ./deploy-app.sh mrvshop production myapp rg-myapp  # explicit names

TENANT=${1:?"Usage: $0 <tenant-id> [environment] [function-app-name] [resource-group]"}
ENVIRONMENT=${2:-"production"}

# Validate environment
if [[ "$ENVIRONMENT" != "staging" && "$ENVIRONMENT" != "production" ]]; then
  echo "❌ Invalid environment: $ENVIRONMENT (must be 'staging' or 'production')"
  exit 1
fi

# Derive names based on environment (matches Bicep naming convention)
if [ "$ENVIRONMENT" == "staging" ]; then
  FUNCTION_APP_NAME=${3:-"${TENANT}-func-staging"}
  RESOURCE_GROUP=${4:-"rg-${TENANT}-staging"}
else
  FUNCTION_APP_NAME=${3:-"${TENANT}-func"}
  RESOURCE_GROUP=${4:-"rg-${TENANT}-prod"}
fi

echo "==> [Buddy ShopAI] Deploying app for tenant: $TENANT"
echo "    Environment: $ENVIRONMENT"
echo "    Function App: $FUNCTION_APP_NAME"
echo "    Resource Group: $RESOURCE_GROUP"

echo "==> Building project..."
dotnet publish --configuration Release --output ./publish

echo "==> Creating ZIP package..."
cd publish
rm -f /tmp/func-deploy.zip
zip -r /tmp/func-deploy.zip .
cd ..

echo "==> Ensuring WEBSITE_RUN_FROM_PACKAGE is not set..."
az functionapp config appsettings delete \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --setting-names WEBSITE_RUN_FROM_PACKAGE 2>/dev/null || true

echo "==> Deploying to Azure Functions: $FUNCTION_APP_NAME"
az functionapp deployment source config-zip \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --src /tmp/func-deploy.zip

echo "==> Cleaning up..."
rm -f /tmp/func-deploy.zip

echo "==> ✅ Deployment complete for tenant: $TENANT"
