// Azure SQL logical server with one or more databases.
// Instantiated twice from main.bicep so PHI isolation is enforced at the
// server level: the Patient Health database never shares a server with
// account/business data. Schema is deployed separately via DACPAC/sqlpackage
// from the SQL Database Projects in /database — never via EF Core migrations.

@description('Azure region for the server and its databases.')
param location string

@description('Tags applied to all resources in this module.')
param tags object = {}

@description('Globally unique name of the SQL logical server (lowercase).')
param serverName string

@description('Administrator login for the server.')
param administratorLogin string

@description('Administrator password for the server. Never committed — injected at deploy time.')
@secure()
param administratorLoginPassword string

@description('Names of the databases to create on this server.')
param databaseNames array

@description('Database SKU name (capacity tier identifier, not a monetary value).')
param databaseSkuName string = 'S0'

@description('Allow Azure services (App Service, Functions) to reach the server through the Azure-internal firewall rule.')
param allowAzureServices bool = true

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// 0.0.0.0 is the Azure-internal sentinel rule ("Allow Azure services and
// resources to access this server"), not an open-internet rule.
resource allowAzureServicesRule 'Microsoft.Sql/servers/firewallRules@2021-11-01' = if (allowAzureServices) {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource databases 'Microsoft.Sql/servers/databases@2021-11-01' = [
  for databaseName in databaseNames: {
    parent: sqlServer
    name: databaseName
    location: location
    tags: tags
    sku: {
      name: databaseSkuName
    }
    properties: {
      collation: 'SQL_Latin1_General_CP1_CI_AS'
      zoneRedundant: false
    }
  }
]

output serverName string = sqlServer.name
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseNames array = databaseNames
