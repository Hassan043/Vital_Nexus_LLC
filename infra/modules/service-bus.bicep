// Azure Service Bus namespace, background-workflow topics, and secure access
// for Dapr pub/sub in deployed Azure environments.

@description('Azure region for Service Bus resources.')
param location string

@description('Tags applied to all resources in this module.')
param tags object = {}

@description('Globally unique Service Bus namespace name.')
param namespaceName string

@description('Service Bus SKU tier (Standard is the lowest tier that supports topics).')
@allowed([
  'Standard'
  'Premium'
])
param skuName string = 'Standard'

@description('Default message time-to-live applied to background workflow topics.')
param topicMessageTimeToLive string = 'P7D'

@description('Dapr pub/sub topic names provisioned for background workflows.')
param topicNames array = [
  'ai-analysis-queue'
  'ai-analysis-completed'
  'ai-analysis-failed'
  'payment-event-received'
  'notification-requested'
  'export-requested'
  'retention-scan-requested'
]

@description('Managed identity principal IDs granted Azure Service Bus Data Sender on the namespace.')
param dataSenderPrincipalIds array = []

@description('Managed identity principal IDs granted Azure Service Bus Data Receiver on the namespace.')
param dataReceiverPrincipalIds array = []

var serviceBusDataSenderRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4ec4b6f8-8174-4080-8749-5d9e3ffdec2'
)

var serviceBusDataReceiverRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '5d8b64ae-25b0-4163-ae76-ad7020dcb3e'
)

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

resource backgroundTopics 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = [
  for topicName in topicNames: {
    parent: serviceBusNamespace
    name: topicName
    properties: {
      enablePartitioning: false
      defaultMessageTimeToLive: topicMessageTimeToLive
    }
  }
]

resource dataSenderAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in dataSenderPrincipalIds: {
    name: guid(serviceBusNamespace.id, principalId, serviceBusDataSenderRoleId, 'sender')
    scope: serviceBusNamespace
    properties: {
      roleDefinitionId: serviceBusDataSenderRoleId
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]

resource dataReceiverAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in dataReceiverPrincipalIds: {
    name: guid(serviceBusNamespace.id, principalId, serviceBusDataReceiverRoleId, 'receiver')
    scope: serviceBusNamespace
    properties: {
      roleDefinitionId: serviceBusDataReceiverRoleId
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]

output namespaceName string = serviceBusNamespace.name
output namespaceId string = serviceBusNamespace.id
output namespaceEndpoint string = serviceBusNamespace.properties.serviceBusEndpoint
output topicNames array = topicNames
output aiAnalysisRequestedTopicName string = 'ai-analysis-queue'
@secure()
output primaryConnectionString string = serviceBusNamespace.listKeys().primaryConnectionString
