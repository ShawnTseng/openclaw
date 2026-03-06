param location string = 'eastasia'
param appNamePrefix string = 'mrvshop'

// Azure OpenAI 區域（gpt-4o-mini GlobalStandard 在 eastasia 不可用，japaneast 為最近可用區域）
param openAILocation string = 'japaneast'

// 'staging' | 'production'
// staging    → resources get a '-staging' suffix, deployed to rg-{customer}-staging
// production → no suffix,                         deployed to rg-{customer}-prod
@allowed(['staging', 'production'])
param environment string = 'production'

@secure()
param lineChannelAccessToken string
@secure()
param lineChannelSecret string

@secure()
param azureOpenAIApiKey string = ''

var isStaging      = environment == 'staging'
var envSuffix      = isStaging ? '-staging' : ''       // resource name suffix
var envSuffixShort = isStaging ? 'staging' : 'prod'    // used in names with length limits

param tags object = {
  Environment: environment
  Project: 'BuddyShopAI'
  ManagedBy: 'Bicep'
}

var uniqueSuffix      = uniqueString(resourceGroup().id)
// Production: preserve original naming (appNamePrefix + 8 chars) for backward compatibility
// Staging:    use 'staging' prefix to differentiate
var storageAccountName = isStaging ? '${appNamePrefix}staging${take(uniqueSuffix, 4)}' : '${appNamePrefix}${take(uniqueSuffix, 8)}'
var functionAppName    = '${appNamePrefix}-func${envSuffix}'
var keyVaultName       = 'kv${replace(appNamePrefix, '-', '')}${envSuffixShort}${take(uniqueSuffix, 3)}'
var openAIName         = '${appNamePrefix}-openai-${envSuffixShort}'

// === Core Resources ===

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    name: storageAccountName
    location: location
    tags: tags
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    name: keyVaultName
    location: location
    tags: tags
  }
}

module openai 'modules/azureopenai.bicep' = {
  name: 'azureopenai'
  params: {
    name: openAIName
    location: openAILocation
    tags: tags
  }
}

module appInsights 'modules/applicationInsights.bicep' = {
  name: 'applicationinsights'
  params: {
    name: '${appNamePrefix}-appinsights${envSuffix}'
    location: location
    tags: tags
  }
}


// === Function App (Consumption Plan for staging, Flex Consumption for production) ===

module functionApp 'modules/functionApp.bicep' = {
  name: 'functionapp'
  params: {
    name: functionAppName
    location: location
    tags: tags
    storageConnectionString: storage.outputs.connectionString
    storageAccountName: storageAccountName
    storageBlobEndpoint: storage.outputs.blobEndpoint
    useFlexConsumption: !isStaging
    appSettings: [
      {
        name: 'TENANT_ID'
        value: appNamePrefix
      }
      {
        name: 'LINE__ChannelAccessToken'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.uri}secrets/LINE-ChannelAccessToken/)'
      }
      {
        name: 'LINE__ChannelSecret'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.uri}secrets/LINE-ChannelSecret/)'
      }
      {
        name: 'AzureOpenAI__Endpoint'
        value: openai.outputs.endpoint
      }
      {
        name: 'AzureOpenAI__ApiKey'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.uri}secrets/AzureOpenAI-ApiKey/)'
      }
      {
        name: 'AzureOpenAI__DeploymentName'
        value: 'gpt-4o-mini'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: appInsights.outputs.connectionString
      }
      {
        name: 'ApplicationInsights__SamplingPercentage'
        value: '90'
      }
    ]
  }
}

// === Key Vault Secrets ===

resource lineAccessTokenSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'LINE-ChannelAccessToken'
  parent: existingKeyVault
  properties: {
    value: lineChannelAccessToken
  }
  dependsOn: [keyVault]
}

resource lineSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'LINE-ChannelSecret'
  parent: existingKeyVault
  properties: {
    value: lineChannelSecret
  }
  dependsOn: [keyVault]
}

resource azureOpenAIApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'AzureOpenAI-ApiKey'
  parent: existingKeyVault
  properties: {
    value: azureOpenAIApiKey != '' ? azureOpenAIApiKey : openai.outputs.apiKey
  }
  dependsOn: [keyVault]
}

// === RBAC Assignments ===

var keyVaultSecretsUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource existingKeyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource functionAppKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: existingKeyVault
  name: guid(keyVaultName, functionAppName, keyVaultSecretsUserRole)
  properties: {
    roleDefinitionId: keyVaultSecretsUserRole
    principalId: functionApp.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// === Outputs ===

output functionAppName string = functionApp.outputs.name
output functionAppUrl string = functionApp.outputs.url
output keyVaultName string = keyVault.outputs.name
output storageAccountName string = storage.outputs.name
output openAIName string = openai.outputs.name
output openAIEndpoint string = openai.outputs.endpoint
output appInsightsName string = appInsights.outputs.name
output appInsightsConnectionString string = appInsights.outputs.connectionString
output resourceGroupName string = resourceGroup().name
