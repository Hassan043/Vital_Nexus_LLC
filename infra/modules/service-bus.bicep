// Azure Service Bus namespace and initial pub/sub topic for Dapr messaging.

@description('Azure region for Service Bus resources.')
param location string

@description('Tags applied to all resources in this module.')
param tags object = {}

@description('Globally unique Service Bus namespace name.')
param namespaceName string

@description('Service Bus SKU tier (Standard required for topics).')
@allowed([
  'Standard'
  'Premium'
])
param skuName string = 'Standard'

@description('Initial Dapr pub/sub topic for AI analysis queue messages.')
param aiAnalysisTopicName string = 'ai-analysis-queue'

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
  }
}

resource aiAnalysisTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: aiAnalysisTopicName
  properties: {
    enablePartitioning: false
  }
}

output namespaceName string = serviceBusNamespace.name
output namespaceId string = serviceBusNamespace.id
output aiAnalysisTopicName string = aiAnalysisTopic.name
@secure()
output primaryConnectionString string = serviceBusNamespace.listKeys().primaryConnectionString
