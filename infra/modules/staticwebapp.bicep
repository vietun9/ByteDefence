@description('Name of the Static Web App')
param name string

@description('Location for the Static Web App')
param location string

@description('API URL for the backend')
param apiUrl string

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: name
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      appLocation: 'src/ByteDefence.Web'
      outputLocation: 'wwwroot'
    }
  }
}

resource staticWebAppSettings 'Microsoft.Web/staticSites/config@2023-01-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    Api__Url: apiUrl
  }
}

output url string = 'https://${staticWebApp.properties.defaultHostname}'
output name string = staticWebApp.name
