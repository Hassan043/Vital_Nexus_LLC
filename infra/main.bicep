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

@description('App Service plan SKU name for the backend API (capacity tier identifier, not a monetary value).')
param apiPlanSkuName string = 'B1'

@description('Azure SQL database SKU name (capacity tier identifier, not a monetary value).')
param sqlDatabaseSkuName string = 'S0'

var tags = {
  product: 'VitalNexus'
  environment: environmentName
  managedBy: 'bicep'
}

// Backend API hosting: Linux App Service running the .NET 8 ASP.NET Core Web API.
module apiAppService 'modules/app-service.bicep' = {
  name: 'api-app-service'
  params: {
    location: location
    tags: tags
    planName: 'asp-${namePrefix}-api-${environmentName}'
    appName: 'app-${namePrefix}-api-${environmentName}-${uniqueString(resourceGroup().id)}'
    skuName: apiPlanSkuName
    aspnetcoreEnvironment: environmentName == 'prod' ? 'Production' : 'Development'
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

// Core data: account/business, function operations, and lab marker reference
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
      'AccountBusiness'
      'FunctionOperations'
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

output apiAppServiceName string = apiAppService.outputs.appName
output apiAppServiceHostname string = apiAppService.outputs.defaultHostname
output apiAppServicePrincipalId string = apiAppService.outputs.principalId
output functionAppName string = functionApp.outputs.functionAppName
output functionAppHostname string = functionApp.outputs.defaultHostname
output functionAppPrincipalId string = functionApp.outputs.principalId
output functionStorageAccountName string = functionApp.outputs.storageAccountName
output coreSqlServerName string = coreSql.outputs.serverName
output coreSqlServerFqdn string = coreSql.outputs.serverFqdn
output phiSqlServerName string = phiSql.outputs.serverName
output phiSqlServerFqdn string = phiSql.outputs.serverFqdn
