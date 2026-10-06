@description('The primary location for all resources.')
param location string = resourceGroup().location

@description('The unique prefix for naming resources.')
param resourcePrefix string = 'stapp'

@description('The administrator login username for the SQL server.')
param sqlAdministratorLogin string

@description('The administrator login password for the SQL server.')
@secure()
param sqlAdministratorLoginPassword string

@description('The deployment target for the web layer.')
@allowed([
  'app-service'
  'container-apps'
])
param deploymentTarget string = 'container-apps'

@description('Creates or explicitly repairs the public Container App bootstrap revisions. Keep false for infrastructure-only reruns after application images have been deployed.')
param manageContainerApps bool = true

@description('The database provider to provision and configure.')
@allowed([
  'SqlServer'
  'PostgreSql'
])
param databaseProvider string = 'SqlServer'

@description('Optional Azure Cache for Redis connection string. When empty, cache falls back to the configured non-Azure provider.')
@secure()
param azureCacheConnectionString string = ''

// Resource Names
var sqlServerName = '${resourcePrefix}-sql-${uniqueString(resourceGroup().id)}'
var logAnalyticsName = '${resourcePrefix}-law-${uniqueString(resourceGroup().id)}'
var acaEnvName = '${resourcePrefix}-env-${uniqueString(resourceGroup().id)}'
var apiAppName = '${resourcePrefix}-api'
var uiAppName = '${resourcePrefix}-ui'
var appServicePlanName = '${resourcePrefix}-asp'
var redisName = '${resourcePrefix}-redis-${uniqueString(resourceGroup().id)}'
var appInsightsName = '${resourcePrefix}-ai-${uniqueString(resourceGroup().id)}'
var keyVaultName = take('${resourcePrefix}kv${uniqueString(resourceGroup().id)}', 24)
var keyVaultUri = 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/'
var storageAccountName = take('${resourcePrefix}st${uniqueString(resourceGroup().id)}', 24)
#disable-next-line BCP318
var apiPrincipalId = deploymentTarget == 'container-apps' ? (containerApps.outputs.?apiPrincipalId ?? '') : (appService.outputs.?apiPrincipalId ?? '')

module sql '../modules/sql.bicep' = if (databaseProvider == 'SqlServer') {
  name: 'sqlDeploy'
  params: {
    serverName: sqlServerName
    databaseName: 'ST'
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
    location: location
    allowAzureIps: true
  }
}

module postgres '../modules/postgres.bicep' = if (databaseProvider == 'PostgreSql') {
  name: 'postgresDeploy'
  params: {
    serverName: sqlServerName // Using same naming convention for simplicity
    databaseName: 'bt'
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
    location: location
    allowAzureIps: true
  }
}

module logAnalytics '../modules/loganalytics.bicep' = {
  name: 'logAnalyticsDeploy'
  params: {
    logAnalyticsWorkspaceName: logAnalyticsName
    location: location
  }
}

module containerApps '../modules/containerapps.bicep' = if (deploymentTarget == 'container-apps') {
  name: 'acaDeploy'
  params: {
    environmentName: acaEnvName
    logAnalyticsCustomerId: logAnalytics.outputs.customerId
    logAnalyticsSharedKey: logAnalytics.outputs.primarySharedKey
    apiAppName: apiAppName
    uiAppName: uiAppName
    location: location
    databaseProvider: databaseProvider
    keyVaultUri: keyVaultUri
    storageAccountName: storageAccountName
    dataProtectionKeyIdentifier: '${keyVaultUri}keys/dataprotection'
    manageContainerApps: manageContainerApps
  }
}

module appService '../modules/appservice.bicep' = if (deploymentTarget == 'app-service') {
  name: 'appServiceDeploy'
  params: {
    appServicePlanName: appServicePlanName
    apiAppName: apiAppName
    uiAppName: uiAppName
    location: location
    databaseProvider: databaseProvider
    keyVaultUri: keyVaultUri
    storageAccountName: storageAccountName
    dataProtectionKeyIdentifier: '${keyVaultUri}keys/dataprotection'
  }
}

module redis '../modules/redis.bicep' = if (empty(azureCacheConnectionString)) {
  name: 'redisDeploy'
  params: {
    redisCacheName: redisName
    location: location
  }
}

module appInsights '../modules/appinsights.bicep' = {
  name: 'appInsightsDeploy'
  params: {
    appInsightsName: appInsightsName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.logAnalyticsWorkspaceId
  }
}

var serviceBusName = '${resourcePrefix}-sb-${uniqueString(resourceGroup().id)}'

module serviceBus '../modules/servicebus.bicep' = {
  name: 'serviceBusDeploy'
  params: {
    namespaceName: serviceBusName
    location: location
    sku: 'Standard'
  }
}

var communicationServiceName = '${resourcePrefix}-email-${uniqueString(resourceGroup().id)}'

module communication '../modules/communication.bicep' = {
  name: 'communicationDeploy'
  params: {
    communicationServiceName: communicationServiceName
    emailServiceName: '${communicationServiceName}-es'
    location: 'global'
  }
}

module keyVault '../modules/keyvault.bicep' = {
  name: 'keyVaultDeploy'
  params: {
    keyVaultName: keyVaultName
    location: location
    apiPrincipalId: apiPrincipalId
    databaseConnectionString: databaseProvider == 'SqlServer'
#disable-next-line BCP318
      ? 'Server=tcp:${sql.outputs.?serverFullyQualifiedDomainName ?? ''},1433;Initial Catalog=${sql.outputs.?databaseName ?? ''};Persist Security Info=False;User ID=${sqlAdministratorLogin};Password=${sqlAdministratorLoginPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
#disable-next-line BCP318
      : 'Host=${postgres.outputs.?serverFullyQualifiedDomainName ?? ''};Port=5432;Database=${postgres.outputs.?databaseName ?? ''};Username=${sqlAdministratorLogin};Password=${sqlAdministratorLoginPassword};Ssl Mode=Require;Trust Server Certificate=false;'
    serviceBusNamespaceName: serviceBus.outputs.serviceBusNamespaceName
    azureCacheConnectionString: !empty(azureCacheConnectionString) ? azureCacheConnectionString : redis.outputs.redisConnectionString
    communicationConnectionString: communication.outputs.communicationConnectionString
    communicationFromAddress: communication.outputs.communicationFromAddress
    appInsightsConnectionString: appInsights.outputs.appInsightsConnectionString
  }
}

module storageAccount '../modules/storage.bicep' = {
  name: 'storageAccountDeploy'
  params: {
    storageAccountName: storageAccountName
    location: location
    apiPrincipalId: apiPrincipalId
  }
}

#disable-next-line BCP318
output sqlServerFqdn string = databaseProvider == 'SqlServer' ? (sql.outputs.?serverFullyQualifiedDomainName ?? '') : (postgres.outputs.?serverFullyQualifiedDomainName ?? '')
#disable-next-line BCP318
output sqlDatabaseName string = databaseProvider == 'SqlServer' ? (sql.outputs.?databaseName ?? '') : (postgres.outputs.?databaseName ?? '')

#disable-next-line BCP318
output apiHost string = deploymentTarget == 'container-apps' ? (containerApps.outputs.?apiFqdn ?? '') : (appService.outputs.?apiDefaultHostName ?? '')
#disable-next-line BCP318
output uiHost string = deploymentTarget == 'container-apps' ? (containerApps.outputs.?uiFqdn ?? '') : (appService.outputs.?uiDefaultHostName ?? '')
