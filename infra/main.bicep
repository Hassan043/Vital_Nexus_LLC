// VitalNexus infrastructure entry point.
// Deployed by .github/workflows/deploy-infra.yml into rg-vitalnexus-<env> with
// the matching main.<env>.bicepparam file.

@description('Deployment environment (matches the bicepparam files and the deploy workflow).')
@allowed([
  'dev'
  'prod'
])
param environmentName string

@description('Short prefix applied to all resource names.')
@minLength(2)
@maxLength(8)
param namePrefix string = 'vnx'

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Administrator login for the Azure SQL servers.')
param sqlAdministratorLogin string

@description('Administrator password for the Azure SQL servers. Never committed — injected from the SQL_ADMIN_PASSWORD environment variable at deploy time.')
@secure()
param sqlAdministratorPassword string

@description('Azure SQL database SKU name (capacity tier identifier, not a monetary value).')
param sqlDatabaseSkuName string = 'S0'

@description('ACR SKU name (capacity tier identifier, not a monetary value).')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param acrSkuName string = 'Basic'

@description('Log Analytics retention in days for the Container Apps environment.')
param logAnalyticsRetentionInDays int = 30

@description('Container image tag applied to frontend, API, and worker images.')
param containerImageTag string = 'latest'

@description('Deploy the payment/Stripe worker container app scaffold.')
param deployPaymentWorker bool = false

@description('Service Bus SKU tier (Standard required for Dapr topic-based pub/sub).')
@allowed([
  'Standard'
  'Premium'
])
param serviceBusSkuName string = 'Standard'

@description('Dapr pub/sub component name used by application code.')
param daprPubSubComponentName string = 'pubsub'

@description('Initial Dapr pub/sub topic for AI analysis queue messages.')
param aiAnalysisTopicName string = 'ai-analysis-queue'

var tags = {
  product: 'VitalNexus'
  environment: environmentName
  managedBy: 'bicep'
}

var nameSuffix = take(uniqueString(resourceGroup().id), 8)
var aspnetcoreEnvironment = environmentName == 'prod' ? 'Production' : 'Development'
var daprPubSubScopedAppIds = [
  'vitalnexus-api'
  'ai-analysis-worker'
]
var daprConfigurationEnvironmentVariables = [
  {
    name: 'Dapr__PubSubComponentName'
    value: daprPubSubComponentName
  }
  {
    name: 'Dapr__AiAnalysisTopicName'
    value: aiAnalysisTopicName
  }
]

// Container hosting foundation: shared ACA environment and image registry.
module acaWorkloadIdentity 'modules/container-apps-workload-identity.bicep' = {
  name: 'aca-workload-identity'
  params: {
    location: location
    tags: tags
    identityName: 'id-${namePrefix}-aca-${environmentName}-${nameSuffix}'
  }
}

module acr 'modules/container-registry.bicep' = {
  name: 'container-registry'
  params: {
    location: location
    tags: tags
    acrName: 'acr${namePrefix}${environmentName}${nameSuffix}'
    skuName: acrSkuName
    acrPullPrincipalIds: [
      acaWorkloadIdentity.outputs.principalId
    ]
  }
}

module acaEnvironment 'modules/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  params: {
    location: location
    tags: tags
    logAnalyticsWorkspaceName: 'log-${namePrefix}-aca-${environmentName}-${nameSuffix}'
    managedEnvironmentName: 'cae-${namePrefix}-${environmentName}-${nameSuffix}'
    logRetentionInDays: logAnalyticsRetentionInDays
  }
}

module frontendApp 'modules/container-app.bicep' = {
  name: 'frontend-container-app'
  params: {
    location: location
    tags: tags
    containerAppName: 'ca-${namePrefix}-frontend-${environmentName}-${nameSuffix}'
    managedEnvironmentId: acaEnvironment.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/vitalnexus-frontend:${containerImageTag}'
    cpu: environmentName == 'prod' ? '0.5' : '0.25'
    memory: environmentName == 'prod' ? '1Gi' : '0.5Gi'
    minReplicas: environmentName == 'prod' ? 1 : 0
    maxReplicas: environmentName == 'prod' ? 5 : 2
    ingressEnabled: true
    externalIngress: true
    targetPort: 8080
    userAssignedIdentityResourceId: acaWorkloadIdentity.outputs.identityId
    acrLoginServer: acr.outputs.acrLoginServer
    environmentVariables: [
      {
        name: 'PORT'
        value: '8080'
      }
    ]
  }
}

module apiApp 'modules/container-app.bicep' = {
  name: 'api-container-app'
  params: {
    location: location
    tags: tags
    containerAppName: 'ca-${namePrefix}-api-${environmentName}-${nameSuffix}'
    managedEnvironmentId: acaEnvironment.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/vitalnexus-api:${containerImageTag}'
    cpu: environmentName == 'prod' ? '1.0' : '0.5'
    memory: environmentName == 'prod' ? '2Gi' : '1Gi'
    minReplicas: 1
    maxReplicas: environmentName == 'prod' ? 10 : 3
    ingressEnabled: true
    externalIngress: true
    targetPort: 8080
    healthProbePath: '/health'
    userAssignedIdentityResourceId: acaWorkloadIdentity.outputs.identityId
    acrLoginServer: acr.outputs.acrLoginServer
    daprEnabled: true
    daprAppId: 'vitalnexus-api'
    daprAppPort: 8080
    environmentVariables: concat(daprConfigurationEnvironmentVariables, [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: aspnetcoreEnvironment
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
    ])
  }
}

module aiAnalysisWorkerApp 'modules/container-app.bicep' = {
  name: 'ai-analysis-worker-container-app'
  params: {
    location: location
    tags: tags
    containerAppName: 'ca-${namePrefix}-ai-worker-${environmentName}-${nameSuffix}'
    managedEnvironmentId: acaEnvironment.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/vitalnexus-ai-analysis-worker:${containerImageTag}'
    cpu: environmentName == 'prod' ? '1.0' : '0.5'
    memory: environmentName == 'prod' ? '2Gi' : '1Gi'
    minReplicas: 1
    maxReplicas: environmentName == 'prod' ? 5 : 2
    ingressEnabled: false
    userAssignedIdentityResourceId: acaWorkloadIdentity.outputs.identityId
    acrLoginServer: acr.outputs.acrLoginServer
    daprEnabled: true
    daprAppId: 'ai-analysis-worker'
    daprAppPort: 8080
    environmentVariables: concat(daprConfigurationEnvironmentVariables, [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: aspnetcoreEnvironment
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
    ])
  }
}

module paymentWorkerApp 'modules/container-app.bicep' = if (deployPaymentWorker) {
  name: 'payment-worker-container-app'
  params: {
    location: location
    tags: tags
    containerAppName: 'ca-${namePrefix}-payment-worker-${environmentName}-${nameSuffix}'
    managedEnvironmentId: acaEnvironment.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/vitalnexus-payment-worker:${containerImageTag}'
    cpu: '0.5'
    memory: '1Gi'
    minReplicas: 0
    maxReplicas: environmentName == 'prod' ? 3 : 1
    ingressEnabled: false
    userAssignedIdentityResourceId: acaWorkloadIdentity.outputs.identityId
    acrLoginServer: acr.outputs.acrLoginServer
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: aspnetcoreEnvironment
      }
    ]
  }
}

// Core data: account/business, and lab marker reference
// data. Never hosts PHI.
module coreSql 'modules/sql-server.bicep' = {
  name: 'core-sql'
  params: {
    location: location
    tags: tags
    serverName: 'sql-${namePrefix}-core-${environmentName}-${uniqueString(resourceGroup().id)}'
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    databaseNames: [
      'Accounts'
      'LabMarkersData'
    ]
    databaseSkuName: sqlDatabaseSkuName
  }
}

// PHI isolation: the Patient Health database lives on its own logical server,
// never sharing one with account/business data.
module phiSql 'modules/sql-server.bicep' = {
  name: 'phi-sql'
  params: {
    location: location
    tags: tags
    serverName: 'sql-${namePrefix}-phi-${environmentName}-${uniqueString(resourceGroup().id)}'
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    databaseNames: [
      'PatientHealth'
    ]
    databaseSkuName: sqlDatabaseSkuName
  }
}

// Application storage: export packages and other app data.
module appStorage 'modules/storage-account.bicep' = {
  name: 'app-storage'
  params: {
    location: location
    tags: tags
    storageAccountName: 'st${namePrefix}app${environmentName}${take(uniqueString(resourceGroup().id), 7)}'
    containerNames: [
      'exports'
    ]
  }
}

// Secrets: RBAC-only Key Vault readable by Container Apps workload identity.
module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    tags: tags
    keyVaultName: 'kv-${namePrefix}-${environmentName}-${take(uniqueString(resourceGroup().id), 7)}'
    secretsUserPrincipalIds: [
      acaWorkloadIdentity.outputs.principalId
    ]
  }
}

// Dapr pub/sub broker for API and worker messaging.
module serviceBus 'modules/service-bus.bicep' = {
  name: 'service-bus'
  params: {
    location: location
    tags: tags
    namespaceName: 'sb-${namePrefix}-${environmentName}-${nameSuffix}'
    skuName: serviceBusSkuName
    topicMessageTimeToLive: environmentName == 'prod' ? 'P14D' : 'P7D'
    dataSenderPrincipalIds: [
      acaWorkloadIdentity.outputs.principalId
    ]
    dataReceiverPrincipalIds: [
      acaWorkloadIdentity.outputs.principalId
    ]
  }
}

module serviceBusConnectionSecret 'modules/key-vault-secret.bicep' = {
  name: 'servicebus-connection-secret'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    secretName: 'servicebus-connection-string'
    secretValue: serviceBus.outputs.primaryConnectionString
  }
}

module daprPubSub 'modules/dapr-pubsub-component.bicep' = {
  name: 'dapr-pubsub-component'
  params: {
    managedEnvironmentName: acaEnvironment.outputs.environmentName
    componentName: daprPubSubComponentName
    connectionString: serviceBus.outputs.primaryConnectionString
    scopedAppIds: daprPubSubScopedAppIds
  }
}

output containerAppsEnvironmentName string = acaEnvironment.outputs.environmentName
output containerAppsEnvironmentId string = acaEnvironment.outputs.environmentId
output logAnalyticsWorkspaceName string = acaEnvironment.outputs.logAnalyticsWorkspaceName
output acrName string = acr.outputs.acrName
output acrLoginServer string = acr.outputs.acrLoginServer
output containerAppsWorkloadIdentityName string = acaWorkloadIdentity.outputs.identityName
output containerAppsWorkloadIdentityPrincipalId string = acaWorkloadIdentity.outputs.principalId
output containerAppsWorkloadIdentityClientId string = acaWorkloadIdentity.outputs.clientId
output frontendContainerAppName string = frontendApp.outputs.containerAppName
output frontendContainerAppFqdn string = frontendApp.outputs.fqdn
output apiContainerAppName string = apiApp.outputs.containerAppName
output apiContainerAppFqdn string = apiApp.outputs.fqdn
output aiAnalysisWorkerContainerAppName string = aiAnalysisWorkerApp.outputs.containerAppName
output paymentWorkerContainerAppName string = deployPaymentWorker ? paymentWorkerApp!.outputs.containerAppName : ''
output coreSqlServerName string = coreSql.outputs.serverName
output coreSqlServerFqdn string = coreSql.outputs.serverFqdn
output phiSqlServerName string = phiSql.outputs.serverName
output phiSqlServerFqdn string = phiSql.outputs.serverFqdn
output appStorageAccountName string = appStorage.outputs.storageAccountName
output appStorageBlobEndpoint string = appStorage.outputs.blobEndpoint
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri
output serviceBusNamespaceName string = serviceBus.outputs.namespaceName
output serviceBusNamespaceEndpoint string = serviceBus.outputs.namespaceEndpoint
output serviceBusTopicNames array = serviceBus.outputs.topicNames
output serviceBusAiAnalysisRequestedTopicName string = serviceBus.outputs.aiAnalysisRequestedTopicName
output serviceBusTopicName string = serviceBus.outputs.aiAnalysisRequestedTopicName
output serviceBusConnectionSecretName string = serviceBusConnectionSecret.outputs.secretName
output daprPubSubComponentName string = daprPubSub.outputs.componentName
