@description('The primary location for all resources.')
param location string = resourceGroup().location

@description('The unique prefix for naming resources.')
param resourcePrefix string = 'stapp'

var acrName = '${resourcePrefix}acr${uniqueString(resourceGroup().id)}'

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

output acrLoginServer string = acr.properties.loginServer
output acrName string = acr.name
