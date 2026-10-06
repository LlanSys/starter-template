@description('Name of the Container Apps Environment')
param environmentName string

@description('The Customer ID of the Log Analytics Workspace')
param logAnalyticsCustomerId string

@description('The Shared Key of the Log Analytics Workspace')
@secure()
param logAnalyticsSharedKey string


@description('Name of the API Container App')
param apiAppName string

@description('Name of the Blazor UI Container App')
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

@description('Creates or repairs the public bootstrap Container Apps. Set to false after the first successful deployment so infrastructure-only runs do not replace deployed application revisions.')
param manageContainerApps bool = true

@description('The Grafana Cloud OTLP endpoint.')
param otlpEndpoint string = ''

@description('The Grafana Cloud OTLP headers.')
@secure()
param otlpHeaders string = ''



// Container Apps Environment
resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
  }
}

// Public bootstrap image used only for the initial app creation or an explicit repair.
// Normal image and registry changes are owned by the deployment workflow.
resource apiApp 'Microsoft.App/containerApps@2023-05-01' = if (manageContainerApps) {
  name: apiAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      // Bootstrap with a public image. The deployment workflow configures the
      // selected private registry only when it replaces this revision.
      ingress: {
        external: true
        targetPort: 8080
      }
    }
    template: {
      containers: [
        {
          name: 'api'
          image: 'mcr.microsoft.com/dotnet/samples:aspnetapp'
          env: [
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
              value: 'https://${uiAppName}.${containerAppEnv.properties.defaultDomain}'
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
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 10
      }
    }
  }
}

// Public bootstrap image used only for the initial app creation or an explicit repair.
// Normal image and registry changes are owned by the deployment workflow.
resource uiApp 'Microsoft.App/containerApps@2023-05-01' = if (manageContainerApps) {
  name: uiAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      // Bootstrap with a public image. The deployment workflow configures the
      // selected private registry only when it replaces this revision.
      ingress: {
        external: true
        targetPort: 8080
        stickySessions: {
          affinity: 'sticky'
        }
      }
    }
    template: {
      containers: [
        {
          name: 'ui'
          image: 'mcr.microsoft.com/dotnet/samples:aspnetapp'
          env: [
            {
              name: 'BackendApi__BaseUrl'
              value: 'https://${apiAppName}.${containerAppEnv.properties.defaultDomain}/'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 10
      }
    }
  }
}

resource existingApiApp 'Microsoft.App/containerApps@2023-05-01' existing = if (!manageContainerApps) {
  name: apiAppName
}

resource existingUiApp 'Microsoft.App/containerApps@2023-05-01' existing = if (!manageContainerApps) {
  name: uiAppName
}

#disable-next-line BCP318
var apiPrincipalId = manageContainerApps ? apiApp.identity.principalId : existingApiApp.identity.principalId
#disable-next-line BCP318
var uiPrincipalId = manageContainerApps ? uiApp.identity.principalId : existingUiApp.identity.principalId
#disable-next-line BCP318
var apiFqdn = manageContainerApps ? apiApp.properties.configuration.ingress.fqdn : existingApiApp.properties.configuration.ingress.fqdn
#disable-next-line BCP318
var uiFqdn = manageContainerApps ? uiApp.properties.configuration.ingress.fqdn : existingUiApp.properties.configuration.ingress.fqdn



output apiFqdn string = apiFqdn
output uiFqdn string = uiFqdn
output apiPrincipalId string = apiPrincipalId
output uiPrincipalId string = uiPrincipalId
