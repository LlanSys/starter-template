@description('The name of the SQL logical server.')
param serverName string

@description('The name of the SQL Database.')
param databaseName string = 'ST'

@description('The administrator login username for the SQL server.')
param administratorLogin string

@description('The administrator login password for the SQL server.')
@secure()
param administratorLoginPassword string

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Allow Azure services and resources to access this server')
param allowAzureIps bool = true

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: serverName
  location: location
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    zoneRedundant: false
  }
}

resource allowAllWindowsAzureIps 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = if (allowAzureIps) {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output serverFullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name
