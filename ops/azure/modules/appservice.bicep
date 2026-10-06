@description('Name of the App Service Plan')
param appServicePlanName string

@description('Name of the API Web App')
param apiAppName string

@description('Name of the UI Web App')
param uiAppName string

@description('Location for all resources.')
param location string = resourceGroup().location

@description('The database provider to configure for the API.')
param databaseProvider string

@description('The URI of the Key Vault (optional)')
param keyVaultUri string = ''

@description('The name of the Storage Account (optional)')
param storageAccountName string = ''

@description('The versionless Key Vault key identifier used to encrypt Data Protection keys.')
param dataProtectionKeyIdentifier string = ''

@description('The Grafana Cloud OTLP endpoint.')
param otlpEndpoint string = ''

@description('The Grafana Cloud OTLP headers.')
@secure()
param otlpHeaders string = ''

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
  }
}

resource apiApp 'Microsoft.Web/sites@2022-09-01' = {
  name: apiAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      appSettings: [
        {
          name: 'DatabaseSettings__Provider'
          value: databaseProvider
        }
        {
          name: 'KeyVault__Uri'
          value: keyVaultUri
        }
        {
          name: 'ProfileImageStorage__AzureBlob__ContainerUri'
          value: empty(storageAccountName) ? '' : 'https://${storageAccountName}.blob.${environment().suffixes.storage}/profile-images'
        }
        {
          name: 'ProfileImageStorage__Provider'
          value: 'AzureBlob'
        }
        {
          name: 'DataProtection__BlobKeyUri'
          value: empty(storageAccountName) ? '' : 'https://${storageAccountName}.blob.${environment().suffixes.storage}/dataprotection-keys/keyring.xml'
        }
        {
          name: 'DataProtection__KeyEncryptionMode'
          value: 'KeyVault'
        }
        {
          name: 'DataProtection__KeyVaultKeyIdentifier'
          value: dataProtectionKeyIdentifier
        }
        {
          name: 'CacheSettings__Provider'
          value: 'Auto'
        }
        {
          name: 'AllowedOrigins__0'
          value: 'https://${uiAppName}.azurewebsites.net'
        }
        {
          name: 'Messaging__Transport'
          value: 'AzureServiceBus'
        }
        {
          name: 'Observability__Otlp__Endpoint'
          value: otlpEndpoint
        }
        {
          name: 'Observability__Otlp__Headers'
          value: otlpHeaders
        }
      ]
    }
  }
}

resource uiApp 'Microsoft.Web/sites@2022-09-01' = {
  name: uiAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      appSettings: [
        {
          name: 'BackendApi__BaseUrl'
          value: 'https://${apiAppName}.azurewebsites.net/'
        }
      ]
    }
  }
}

output apiDefaultHostName string = apiApp.properties.defaultHostName
output uiDefaultHostName string = uiApp.properties.defaultHostName
output apiPrincipalId string = apiApp.identity.principalId
output uiPrincipalId string = uiApp.identity.principalId
