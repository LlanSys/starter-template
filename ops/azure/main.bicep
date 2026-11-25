@description('The primary location for all resources.')
param location string = resourceGroup().location

@description('The unique prefix for naming resources.')
param resourcePrefix string = 'stapp'

@description('The administrator login username for the database server.')
param sqlAdministratorLogin string

@description('The administrator login password for the database server.')
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

@description('The database provider to provision and configure. Empty selects the first supported provider.')
@allowed([
  ''
  // <db:SqlServer>
  'SqlServer'
  // </db:SqlServer>
  // <db:PostgreSql>
  'PostgreSql'
  // </db:PostgreSql>
])
param databaseProvider string = ''

var supportedDatabaseProviders = [
  // <db:SqlServer>
  'SqlServer'
  // </db:SqlServer>
  // <db:PostgreSql>
  'PostgreSql'
  // </db:PostgreSql>
]
var selectedDatabaseProvider = empty(databaseProvider) ? first(supportedDatabaseProviders) : databaseProvider

@description('Optional Azure Cache for Redis connection string. When empty, cache falls back to the configured non-Azure provider.')
@secure()
param azureCacheConnectionString string = ''

@description('The Grafana Cloud OTLP endpoint.')
param otlpEndpoint string = ''

@description('The Grafana Cloud OTLP headers (e.g. Authorization=Basic ...)')
@secure()
param otlpHeaders string = ''

@description('Observability mode. azure-monitor: provisions Log Analytics and Application Insights and stores Container Apps platform logs in Log Analytics. otlp-only: provisions neither; application logs, traces and metrics go only to the OTLP endpoint (set otlpEndpoint), and Container Apps platform logs are not stored, although live log streaming still works.')
@allowed([
  'azure-monitor'
  'otlp-only'
])
param observabilityMode string = 'azure-monitor'
var useAzureMonitor = observabilityMode == 'azure-monitor'

// Resource Names
var databaseServerName = '${resourcePrefix}-sql-${uniqueString(resourceGroup().id)}'
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

// <db:SqlServer>
module sql 'modules/sql.bicep' = if (selectedDatabaseProvider == 'SqlServer') {
  name: 'sqlDeploy'
  params: {
    serverName: databaseServerName
    databaseName: 'ST'
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
    location: location
    allowAzureIps: true
  }
}
// </db:SqlServer>

// <db:PostgreSql>
module postgres 'modules/postgres.bicep' = if (selectedDatabaseProvider == 'PostgreSql') {
  name: 'postgresDeploy'
  params: {
    serverName: databaseServerName
    databaseName: 'bt'
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
    location: location
    allowAzureIps: true
  }
}
// </db:PostgreSql>

// Exactly one provider value is non-empty; markers let fork-time selection drop the others.
// <db:SqlServer>
#disable-next-line BCP318
var sqlServerConnectionString = selectedDatabaseProvider == 'SqlServer' ? 'Server=tcp:${sql.outputs.?serverFullyQualifiedDomainName ?? ''},1433;Initial Catalog=${sql.outputs.?databaseName ?? ''};Persist Security Info=False;User ID=${sqlAdministratorLogin};Password=${sqlAdministratorLoginPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;' : ''
#disable-next-line BCP318
var sqlServerFqdn = selectedDatabaseProvider == 'SqlServer' ? (sql.outputs.?serverFullyQualifiedDomainName ?? '') : ''
#disable-next-line BCP318
var sqlServerDatabaseName = selectedDatabaseProvider == 'SqlServer' ? (sql.outputs.?databaseName ?? '') : ''
// </db:SqlServer>
// <db:PostgreSql>
#disable-next-line BCP318
var postgresConnectionString = selectedDatabaseProvider == 'PostgreSql' ? 'Host=${postgres.outputs.?serverFullyQualifiedDomainName ?? ''};Port=5432;Database=${postgres.outputs.?databaseName ?? ''};Username=${sqlAdministratorLogin};Password=${sqlAdministratorLoginPassword};Ssl Mode=Require;Trust Server Certificate=false;' : ''
#disable-next-line BCP318
var postgresFqdn = selectedDatabaseProvider == 'PostgreSql' ? (postgres.outputs.?serverFullyQualifiedDomainName ?? '') : ''
#disable-next-line BCP318
var postgresDatabaseName = selectedDatabaseProvider == 'PostgreSql' ? (postgres.outputs.?databaseName ?? '') : ''
// </db:PostgreSql>
var databaseConnectionStrings = [
  // <db:SqlServer>
  sqlServerConnectionString
  // </db:SqlServer>
  // <db:PostgreSql>
  postgresConnectionString
  // </db:PostgreSql>
]
var databaseServerFqdns = [
  // <db:SqlServer>
  sqlServerFqdn
  // </db:SqlServer>
  // <db:PostgreSql>
  postgresFqdn
  // </db:PostgreSql>
]
var databaseNames = [
  // <db:SqlServer>
  sqlServerDatabaseName
  // </db:SqlServer>
  // <db:PostgreSql>
  postgresDatabaseName
  // </db:PostgreSql>
]

module logAnalytics 'modules/loganalytics.bicep' = if (useAzureMonitor) {
  name: 'logAnalyticsDeploy'
  params: {
    logAnalyticsWorkspaceName: logAnalyticsName
    location: location
  }
}

module containerApps 'modules/containerapps.bicep' = if (deploymentTarget == 'container-apps') {
  name: 'acaDeploy'
  params: {
    environmentName: acaEnvName
    platformLogsToLogAnalytics: useAzureMonitor
    logAnalyticsCustomerId: useAzureMonitor ? logAnalytics!.outputs.customerId : ''
    logAnalyticsSharedKey: useAzureMonitor ? logAnalytics!.outputs.primarySharedKey : ''
    apiAppName: apiAppName
    uiAppName: uiAppName
    location: location
    databaseProvider: selectedDatabaseProvider
    keyVaultUri: keyVaultUri
    storageAccountName: storageAccountName
    dataProtectionKeyIdentifier: '${keyVaultUri}keys/dataprotection'
    manageContainerApps: manageContainerApps
    otlpEndpoint: otlpEndpoint
    otlpHeaders: otlpHeaders
  }
}

module appService 'modules/appservice.bicep' = if (deploymentTarget == 'app-service') {
  name: 'appServiceDeploy'
  params: {
    appServicePlanName: appServicePlanName
    apiAppName: apiAppName
    uiAppName: uiAppName
    location: location
    databaseProvider: selectedDatabaseProvider
    keyVaultUri: keyVaultUri
    storageAccountName: storageAccountName
    dataProtectionKeyIdentifier: '${keyVaultUri}keys/dataprotection'
    otlpEndpoint: otlpEndpoint
    otlpHeaders: otlpHeaders
  }
}

module redis 'modules/redis.bicep' = if (empty(azureCacheConnectionString)) {
  name: 'redisDeploy'
  params: {
    redisCacheName: redisName
    location: location
  }
}

module appInsights 'modules/appinsights.bicep' = if (useAzureMonitor) {
  name: 'appInsightsDeploy'
  params: {
    appInsightsName: appInsightsName
    location: location
    logAnalyticsWorkspaceId: logAnalytics!.outputs.logAnalyticsWorkspaceId
  }
}

var serviceBusName = '${resourcePrefix}-sb-${uniqueString(resourceGroup().id)}'

module serviceBus 'modules/servicebus.bicep' = {
  name: 'serviceBusDeploy'
  params: {
    namespaceName: serviceBusName
    location: location
    sku: 'Standard'
  }
}

var communicationServiceName = '${resourcePrefix}-email-${uniqueString(resourceGroup().id)}'

module communication 'modules/communication.bicep' = {
  name: 'communicationDeploy'
  params: {
    communicationServiceName: communicationServiceName
    emailServiceName: '${communicationServiceName}-es'
    location: 'global'
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyVaultDeploy'
  params: {
    keyVaultName: keyVaultName
    location: location
    apiPrincipalId: apiPrincipalId
    databaseConnectionString: join(databaseConnectionStrings, '')
    serviceBusNamespaceName: serviceBus.outputs.serviceBusNamespaceName
    azureCacheConnectionString: !empty(azureCacheConnectionString) ? azureCacheConnectionString : redis.outputs.redisConnectionString
    communicationConnectionString: communication.outputs.communicationConnectionString
    communicationFromAddress: communication.outputs.communicationFromAddress
    appInsightsConnectionString: useAzureMonitor ? appInsights!.outputs.appInsightsConnectionString : ''
  }
}

module storageAccount 'modules/storage.bicep' = {
  name: 'storageAccountDeploy'
  params: {
    storageAccountName: storageAccountName
    location: location
    apiPrincipalId: apiPrincipalId
  }
}

output databaseServerFqdn string = join(databaseServerFqdns, '')
output databaseName string = join(databaseNames, '')

#disable-next-line BCP318
output apiHost string = deploymentTarget == 'container-apps' ? (containerApps.outputs.?apiFqdn ?? '') : (appService.outputs.?apiDefaultHostName ?? '')
#disable-next-line BCP318
output uiHost string = deploymentTarget == 'container-apps' ? (containerApps.outputs.?uiFqdn ?? '') : (appService.outputs.?uiDefaultHostName ?? '')
