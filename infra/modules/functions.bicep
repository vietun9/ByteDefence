@description('Name of the Function App')
param name string

@description('Location for the Function App')
param location string

@description('Environment name')
param environment string

@description('JWT Secret')
@secure()
param jwtSecret string

@description('SignalR connection string')
@secure()
param signalRConnectionString string

@description('Cosmos DB connection string')
@secure()
param cosmosConnectionString string

// Storage Account for Functions
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'stbytedefence${environment}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

// App Service Plan (Consumption)
resource hostingPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: 'plan-${name}'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${name}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: name
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      cors: {
        allowedOrigins: ['*']
      }
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'UseCosmosDb'
          value: 'true'
        }
        {
          name: 'CosmosDb__ConnectionString'
          value: cosmosConnectionString
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: 'ByteDefence'
        }
        {
          name: 'SignalR__Mode'
          value: 'Azure'
        }
        {
          name: 'SignalR__ConnectionString'
          value: signalRConnectionString
        }
        {
          name: 'Jwt__Secret'
          value: jwtSecret
        }
        {
          name: 'Jwt__Issuer'
          value: 'https://${name}.azurewebsites.net'
        }
        {
          name: 'Jwt__Audience'
          value: 'ByteDefence-API'
        }
      ]
    }
  }
}

output url string = 'https://${functionApp.properties.defaultHostName}'
output name string = functionApp.name
