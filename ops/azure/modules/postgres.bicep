@description('The name of the PostgreSQL server.')
param serverName string

@description('The name of the PostgreSQL Database.')
param databaseName string = 'bt'

@description('The administrator login username for the PostgreSQL server.')
param administratorLogin string

@description('The administrator login password for the PostgreSQL server.')
@secure()
param administratorLoginPassword string

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Allow Azure services and resources to access this server')
param allowAzureIps bool = true

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: serverName
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '15'
    storage: {
      storageSizeGB: 32
    }
  }
}

resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgresServer
  name: databaseName
  properties: {
    charset: 'utf8'
    collation: 'en_US.utf8'
  }
}

resource allowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = if (allowAzureIps) {
  parent: postgresServer
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output serverFullyQualifiedDomainName string = postgresServer.properties.fullyQualifiedDomainName
output databaseName string = postgresDatabase.name
