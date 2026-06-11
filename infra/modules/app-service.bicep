// Linux App Service plan + web app for the VitalNexus backend API (.NET 8).
// App Service was chosen over Container Apps: the API is a single code-deployed
// ASP.NET Core app with no container images in the repo, so App Service keeps
// the pipeline simple (no registry or image builds required).

@description('Azure region for the plan and the web app.')
param location string

@description('Tags applied to all resources in this module.')
param tags object = {}

@description('Name of the App Service plan.')
param planName string

@description('Globally unique name of the web app.')
param appName string

@description('App Service plan SKU name (capacity tier identifier).')
param skuName string = 'B1'

@description('Keep the app loaded at all times. Requires Basic tier or higher; set to false on Free/Shared SKUs.')
param alwaysOn bool = true

@description('Value for the ASPNETCORE_ENVIRONMENT app setting.')
param aspnetcoreEnvironment string = 'Production'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: skuName
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    // System-assigned identity for least-privilege access to Key Vault and SQL
    // in later issues — no credentials stored in app settings.
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      alwaysOn: alwaysOn
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: aspnetcoreEnvironment
        }
      ]
    }
  }
}

output appName string = webApp.name
output defaultHostname string = webApp.properties.defaultHostName
output principalId string = webApp.identity.principalId
