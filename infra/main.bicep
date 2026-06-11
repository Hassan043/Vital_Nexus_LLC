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

// The SQL parameters are part of the stable contract with main.<env>.bicepparam.
// They are consumed by the Azure SQL modules added in later F2.T1 issues.
@description('Administrator login for the Azure SQL servers (used by upcoming SQL modules).')
param sqlAdministratorLogin string

@description('Administrator password for the Azure SQL servers. Never committed — injected from the SQL_ADMIN_PASSWORD environment variable at deploy time.')
@secure()
param sqlAdministratorPassword string

@description('App Service plan SKU name for the backend API (capacity tier identifier, not a monetary value).')
param apiPlanSkuName string = 'B1'

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

output apiAppServiceName string = apiAppService.outputs.appName
output apiAppServiceHostname string = apiAppService.outputs.defaultHostname
output apiAppServicePrincipalId string = apiAppService.outputs.principalId
output functionAppName string = functionApp.outputs.functionAppName
output functionAppHostname string = functionApp.outputs.defaultHostname
output functionAppPrincipalId string = functionApp.outputs.principalId
output functionStorageAccountName string = functionApp.outputs.storageAccountName
