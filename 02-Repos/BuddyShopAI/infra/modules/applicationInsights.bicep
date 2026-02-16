param name string
param location string
param tags object = {}
param workspaceId string = ''

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspaceId != '' ? workspaceId : null
    IngestionMode: workspaceId != '' ? 'LogAnalytics' : 'ApplicationInsights'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    RetentionInDays: 30 // Cost optimization: 30 days retention (free tier supports up to 90)
    SamplingPercentage: 90 // Cost optimization: 90% sampling to reduce data ingestion
  }
}

output id string = appInsights.id
output name string = appInsights.name
output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString
