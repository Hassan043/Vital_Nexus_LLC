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

var tags = {
  product: 'VitalNexus'
  environment: environmentName
  managedBy: 'bicep'
}

var nameSuffix = take(uniqueString(resourceGroup().id), 8)

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

// Background jobs: Linux consumption Function App running the .NET 8 isolated worker.
module functionApp 'modules/function-app.bicep' = {
  name: 'function-app'
  params: {
    location: location
    tags: tags
    planName: 'asp-${namePrefix}-func-${environmentName}'
    functionAppName: 'func-${namePrefix}-${environmentName}-${uniqueString(resourceGroup().id)}'
    storageAccountName: 'st${namePrefix}fn${environmentName}${take(uniqueString(resourceGroup().id), 8)}'
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

// Application storage: export packages and other app data, separate from the
// Functions runtime storage account.
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

// Secrets: RBAC-only Key Vault readable by Container Apps and Functions identities.
module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    tags: tags
    keyVaultName: 'kv-${namePrefix}-${environmentName}-${take(uniqueString(resourceGroup().id), 7)}'
    secretsUserPrincipalIds: [
      acaWorkloadIdentity.outputs.principalId
      functionApp.outputs.principalId
    ]
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
output functionAppName string = functionApp.outputs.functionAppName
output functionAppHostname string = functionApp.outputs.defaultHostname
output functionAppPrincipalId string = functionApp.outputs.principalId
output functionStorageAccountName string = functionApp.outputs.storageAccountName
output coreSqlServerName string = coreSql.outputs.serverName
output coreSqlServerFqdn string = coreSql.outputs.serverFqdn
output phiSqlServerName string = phiSql.outputs.serverName
output phiSqlServerFqdn string = phiSql.outputs.serverFqdn
output appStorageAccountName string = appStorage.outputs.storageAccountName
output appStorageBlobEndpoint string = appStorage.outputs.blobEndpoint
output keyVaultName string = keyVault.outputs.keyVaultName
output keyVaultUri string = keyVault.outputs.keyVaultUri
