param name string
param location string
param tags object = {}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'EP1'
    tier: 'ElasticPremium'
  }
  properties: {
    reserved: false
    maximumElasticWorkerCount: 3
  }
}

output id string = appServicePlan.id
output name string = appServicePlan.name
