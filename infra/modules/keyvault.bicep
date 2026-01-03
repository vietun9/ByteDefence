@description('Name of the Key Vault')
param name string

@description('Location for the Key Vault')
param location string

@description('JWT Secret to store')
@secure()
param jwtSecret string

@description('SignalR Connection String to store')
@secure()
param signalRConnectionString string

@description('Cosmos DB Connection String to store')
@secure()
param cosmosConnectionString string

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: name
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: false
    accessPolicies: []
  }
}

resource jwtSecretResource 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'JwtSecret'
  properties: {
    value: jwtSecret
  }
}

resource signalRSecretResource 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'SignalRConnectionString'
  properties: {
    value: signalRConnectionString
  }
}

resource cosmosSecretResource 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'CosmosConnectionString'
  properties: {
    value: cosmosConnectionString
  }
}

output name string = keyVault.name
output jwtSecretUri string = jwtSecretResource.properties.secretUri
output signalRConnectionStringUri string = signalRSecretResource.properties.secretUri
output cosmosConnectionStringUri string = cosmosSecretResource.properties.secretUri
