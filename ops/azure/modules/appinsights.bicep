@description('The name of the Application Insights component')
param appInsightsName string

@description('The location for the Application Insights component')
param location string

@description('The resource ID of the Log Analytics Workspace to link to')
param logAnalyticsWorkspaceId string

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

output appInsightsConnectionString string = appInsights.properties.ConnectionString
