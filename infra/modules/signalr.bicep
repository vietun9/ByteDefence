@description('Name of the SignalR Service')
param name string

@description('Location for the SignalR Service')
param location string

resource signalR 'Microsoft.SignalRService/signalR@2023-02-01' = {
  name: name
  location: location
  sku: {
    name: 'Free_F1'
    tier: 'Free'
    capacity: 1
  }
  kind: 'SignalR'
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Serverless'
      }
    ]
    cors: {
      allowedOrigins: ['*']
    }
    networkACLs: {
      defaultAction: 'Allow'
    }
  }
}

output connectionString string = signalR.listKeys().primaryConnectionString
output endpoint string = 'https://${signalR.properties.hostName}'
output name string = signalR.name
