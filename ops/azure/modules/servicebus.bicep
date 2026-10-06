@description('The name of the Service Bus namespace')
param namespaceName string

@description('The location of the Service Bus namespace')
param location string

@description('The SKU of the Service Bus namespace')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Standard'

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  sku: {
    name: sku
    tier: sku
  }
}

output serviceBusNamespaceName string = serviceBusNamespace.name
output serviceBusEndpoint string = serviceBusNamespace.properties.serviceBusEndpoint
