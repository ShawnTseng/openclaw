param name string
param location string
param tags object = {}
param deploymentName string = 'gpt-4o-mini'
param modelName string = 'gpt-4o-mini'
param modelVersion string = '2024-07-18'
param skuName string = 'Standard'
param skuCapacity int = 30 // 30K TPM

resource openai 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: name
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

resource model 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openai
  name: deploymentName
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }
  }
}

output name string = openai.name
output endpoint string = openai.properties.endpoint
output id string = openai.id
output apiKey string = openai.listKeys().key1
