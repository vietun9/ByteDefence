@description('Environment name (dev, test, prod)')
param environment string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('JWT Secret for API authentication')
@secure()
param jwtSecret string

// Azure Functions for GraphQL API
module functions 'modules/functions.bicep' = {
  name: 'functions-deployment'
  params: {
    name: 'func-bytedefence-${environment}'
    location: location
    environment: environment
    jwtSecret: jwtSecret
    signalRConnectionString: signalr.outputs.connectionString
    cosmosConnectionString: cosmos.outputs.connectionString
  }
}

// Azure SignalR Service for real-time updates
module signalr 'modules/signalr.bicep' = {
  name: 'signalr-deployment'
  params: {
    name: 'sigr-bytedefence-${environment}'
    location: location
  }
}

// Azure Cosmos DB for data storage
module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmos-deployment'
  params: {
    name: 'cosmos-bytedefence-${environment}'
    location: location
  }
}

// Azure Static Web Apps for Blazor WASM
module staticwebapp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp-deployment'
  params: {
    name: 'swa-bytedefence-${environment}'
    location: location
    apiUrl: functions.outputs.url
  }
}

// Outputs
output functionAppUrl string = functions.outputs.url
output signalREndpoint string = signalr.outputs.endpoint
output staticWebAppUrl string = staticwebapp.outputs.url
