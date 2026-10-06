@description('The name of the Storage Account')
param storageAccountName string

@description('The location of the Storage Account')
param location string

@description('The Principal ID of the API application (to grant access)')
param apiPrincipalId string = ''

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource profileImagesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'profile-images'
  properties: {
    publicAccess: 'None'
  }
}

resource dataProtectionContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'dataprotection-keys'
  properties: {
    publicAccess: 'None'
  }
}

// Built-in Role ID for "Storage Blob Data Contributor"
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

// Grant API App access to Storage Blob Data Contributor
resource apiStorageAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiPrincipalId)) {
  name: guid(storageAccount.id, apiPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output storageAccountName string = storageAccount.name
