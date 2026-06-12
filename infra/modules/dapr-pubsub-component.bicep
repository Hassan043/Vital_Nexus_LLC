// Dapr pub/sub component for Azure Container Apps using Azure Service Bus.

@description('Name of the existing Container Apps managed environment.')
param managedEnvironmentName string

@description('Dapr component name used by application code (for example pubsub).')
param componentName string = 'pubsub'

@description('Azure Service Bus connection string for the pub/sub broker.')
@secure()
param connectionString string

@description('Dapr application IDs allowed to use this pub/sub component.')
param scopedAppIds array

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: managedEnvironmentName
}

resource daprPubSubComponent 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: managedEnvironment
  name: componentName
  properties: {
    componentType: 'pubsub.azure.servicebus.topics'
    version: 'v1'
    ignoreErrors: false
    initTimeout: '60s'
    secrets: [
      {
        name: 'servicebus-connection-string'
        value: connectionString
      }
    ]
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'servicebus-connection-string'
      }
    ]
    scopes: scopedAppIds
  }
}

output componentName string = daprPubSubComponent.name
output componentType string = daprPubSubComponent.properties.componentType
