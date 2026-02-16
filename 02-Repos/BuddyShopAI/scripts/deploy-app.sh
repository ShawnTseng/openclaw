#!/bin/bash
set -e

TENANT=${1:?"Usage: $0 <tenant-id> [function-app-name] [resource-group]"}
FUNCTION_APP_NAME=${2:-"${TENANT}-func"}
RESOURCE_GROUP=${3:-"rg-${TENANT}-prod"}

echo "==> [Buddy ShopAI] Deploying app for tenant: $TENANT"
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
