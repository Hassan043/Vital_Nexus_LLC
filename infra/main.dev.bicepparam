using './main.bicep'

param environmentName = 'dev'
param namePrefix = 'vnx'

// Optional. If omitted, it uses the resource group location.
// param location = 'eastus'

param apiPlanSkuName = 'F1'
param sqlDatabaseSkuName = 'Basic'

// SECURITY: never commit real credentials. Injected at deploy time (empty
// password default so the repo holds no secret). Set before deploying:
//   PowerShell:  $env:SQL_ADMIN_LOGIN = 'vnxadmin'
//                $env:SQL_ADMIN_PASSWORD = '<strong-password>'
//   bash:        export SQL_ADMIN_LOGIN='vnxadmin'
//                export SQL_ADMIN_PASSWORD='<strong-password>'
param sqlAdministratorLogin = readEnvironmentVariable('SQL_ADMIN_LOGIN', 'vnxadmin')
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
