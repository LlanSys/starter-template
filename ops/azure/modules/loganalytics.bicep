@description('Name of the Log Analytics Workspace')
param logAnalyticsWorkspaceName string

@description('Location for all resources.')
param location string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output customerId string = logAnalyticsWorkspace.properties.customerId
// The shared key is not an output: deployment outputs are visible in deployment history.
// Callers read it with listKeys() on an existing-resource reference and pass it to a secure parameter.
