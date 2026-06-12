// Azure Container Apps environment with Log Analytics integration for
// containerized frontend, API, AI worker, and future worker services.

@description('Azure region for the Container Apps environment and Log Analytics.')
param location string

@description('Tags applied to all resources in this module.')
param tags object = {}

@description('Name of the Log Analytics workspace backing ACA diagnostics.')
param logAnalyticsWorkspaceName string

@description('Name of the Azure Container Apps managed environment.')
param managedEnvironmentName string

@description('Log retention period in days.')
param logRetentionInDays int = 30

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: logRetentionInDays
  }
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: managedEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

output environmentName string = managedEnvironment.name
output environmentId string = managedEnvironment.id
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
