param name string
param location string
param tags object = {}
param storageConnectionString string
param storageAccountName string
param storageBlobEndpoint string = ''
param appSettings array = []
param useFlexConsumption bool = false

// === Flex Consumption Hosting Plan ===
resource hostingPlan 'Microsoft.Web/serverfarms@2024-04-01' = if (useFlexConsumption) {
  name: '${name}-plan'
  location: location
  tags: tags
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  kind: 'functionapp'
  properties: {
    reserved: true
  }
}

// === Deployment Package Container (Flex only) ===
resource storageAccountRef 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' existing = {
  name: 'default'
  parent: storageAccountRef
}

resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = if (useFlexConsumption) {
  name: 'deploymentpackage'
  parent: blobService
  properties: {
    publicAccess: 'None'
  }
}

// === Base App Settings ===

// Consumption Plan: 需要 WEBSITE_CONTENT* 與 FUNCTIONS_WORKER_RUNTIME
var consumptionBaseSettings = [
  {
    name: 'AzureWebJobsStorage'
    value: storageConnectionString
  }
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    value: storageConnectionString
  }
  {
    name: 'WEBSITE_CONTENTSHARE'
    value: toLower(name)
  }
  {
    name: 'FUNCTIONS_EXTENSION_VERSION'
    value: '~4'
  }
  {
    name: 'FUNCTIONS_WORKER_RUNTIME'
    value: 'dotnet-isolated'
  }
]

// Flex Consumption: runtime 由 functionAppConfig 管理，只需 AzureWebJobsStorage
var flexBaseSettings = [
  {
    name: 'AzureWebJobsStorage'
    value: storageConnectionString
  }
]

// === Consumption Plan Function App ===
resource functionAppConsumption 'Microsoft.Web/sites@2024-04-01' = if (!useFlexConsumption) {
  name: name
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    reserved: true
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: union(consumptionBaseSettings, appSettings)
    }
  }
}

// === Flex Consumption Plan Function App ===
// - 永遠保持 1 個 HTTP instance 待命，避免冷啟動
// - Runtime 與版本由 functionAppConfig 管理
resource functionAppFlex 'Microsoft.Web/sites@2024-04-01' = if (useFlexConsumption) {
  name: name
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    reserved: true
    httpsOnly: true
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: union(flexBaseSettings, appSettings)
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageBlobEndpoint}deploymentpackage'
          authentication: {
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'AzureWebJobsStorage'
          }
        }
      }
      scaleAndConcurrency: {
        alwaysReady: [
          {
            name: 'http'
            instanceCount: 1
          }
        ]
        maximumInstanceCount: 40
        instanceMemoryMB: 512
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '8.0'
      }
    }
  }
  dependsOn: [deploymentContainer]
}

output name string = name
output url string = useFlexConsumption
  ? 'https://${functionAppFlex!.properties.defaultHostName}'
  : 'https://${functionAppConsumption!.properties.defaultHostName}'
output principalId string = useFlexConsumption
  ? functionAppFlex!.identity.principalId
  : functionAppConsumption!.identity.principalId
