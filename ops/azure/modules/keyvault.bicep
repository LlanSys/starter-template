@description('The name of the Key Vault')
param keyVaultName string

@description('The location of the Key Vault')
param location string

@description('The Principal ID of the API application (to grant access)')
param apiPrincipalId string = ''

@description('The application database connection string stored as a runtime secret.')
@secure()
param databaseConnectionString string

@description('The Azure Service Bus namespace whose connection string is stored as a runtime secret.')
param serviceBusNamespaceName string

@description('Optional Azure Cache for Redis connection string stored as a runtime secret.')
@secure()
param azureCacheConnectionString string = ''

@description('Azure Communication Services Connection String.')
@secure()
param communicationConnectionString string = ''

@description('Azure Communication Services Managed Domain From Address.')
param communicationFromAddress string = ''

@description('Application Insights Connection String.')
@secure()
param appInsightsConnectionString string = ''

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enabledForDeployment: true
    enabledForTemplateDeployment: true
  }
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusNamespaceName
}

resource serviceBusAuthorizationRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2022-10-01-preview' existing = {
  parent: serviceBusNamespace
  name: 'RootManageSharedAccessKey'
}

var serviceBusConnectionString = serviceBusAuthorizationRule.listKeys().primaryConnectionString

// Built-in Role ID for "Key Vault Secrets User"
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var keyVaultCryptoUserRoleId = '12338af0-0e69-4776-bea7-57ae8d297424'

resource dataProtectionKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: 'dataprotection'
  properties: {
    kty: 'RSA'
    keySize: 2048
    keyOps: [
      'wrapKey'
      'unwrapKey'
    ]
  }
}

resource defaultConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--DefaultConnection'
  properties: {
    value: databaseConnectionString
  }
}

resource serviceBusConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Messaging--AzureServiceBus--ConnectionString'
  properties: {
    value: serviceBusConnectionString
  }
}

resource azureCacheConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(azureCacheConnectionString)) {
  parent: keyVault
  name: 'CacheSettings--Azure--ConnectionString'
  properties: {
    value: azureCacheConnectionString
  }
}

resource communicationConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(communicationConnectionString)) {
  parent: keyVault
  name: 'EmailSettings--AzureCommunication--ConnectionString'
  properties: {
    value: communicationConnectionString
  }
}

resource communicationFromAddressSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(communicationFromAddress)) {
  parent: keyVault
  name: 'EmailSettings--FromAddress'
  properties: {
    value: communicationFromAddress
  }
}

resource appInsightsConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(appInsightsConnectionString)) {
  parent: keyVault
  name: 'ApplicationInsights--ConnectionString'
  properties: {
    value: appInsightsConnectionString
  }
}

// Grant API App access to Key Vault Secrets
resource apiKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiPrincipalId)) {
  name: guid(keyVault.id, apiPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource apiKeyVaultCryptoAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiPrincipalId)) {
  name: guid(keyVault.id, apiPrincipalId, keyVaultCryptoUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultCryptoUserRoleId)
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output keyVaultUri string = keyVault.properties.vaultUri
output dataProtectionKeyIdentifier string = '${keyVault.properties.vaultUri}keys/${dataProtectionKey.name}'
