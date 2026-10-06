@description('The name of the Communication Service')
param communicationServiceName string

@description('The name of the Email Service')
param emailServiceName string

@description('The location of the Communication Service (global)')
param location string = 'global'

@description('The data location for the Email Service (United States)')
param dataLocation string = 'United States'

resource emailService 'Microsoft.Communication/emailServices@2023-04-01-preview' = {
  name: emailServiceName
  location: location
  properties: {
    dataLocation: dataLocation
  }
}

resource emailDomain 'Microsoft.Communication/emailServices/domains@2023-04-01-preview' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: location
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource communicationService 'Microsoft.Communication/CommunicationServices@2023-04-01-preview' = {
  name: communicationServiceName
  location: location
  properties: {
    dataLocation: dataLocation
    linkedDomains: [
      emailDomain.id
    ]
  }
}

var connectionString = communicationService.listKeys().primaryConnectionString
var fromAddress = 'DoNotReply@${emailDomain.properties.fromSenderDomain}'

output communicationConnectionString string = connectionString
output communicationFromAddress string = fromAddress
