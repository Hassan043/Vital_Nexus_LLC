using './main.bicep'

param environmentName = 'prod'
param namePrefix = 'vnx'
param sqlAdministratorLogin = 'vnxadmin'
param acrSkuName = 'Standard'
param sqlDatabaseSkuName = 'S0'
param logAnalyticsRetentionInDays = 90
param containerImageTag = 'latest'
param deployPaymentWorker = false

// SECURITY: never commit a real password. The value is read from the
// SQL_ADMIN_PASSWORD environment variable at deploy time (empty default so the
// repo holds no secret). Set it before deploying:
//   PowerShell:  $env:SQL_ADMIN_PASSWORD = '<strong-password>'
//   bash:        export SQL_ADMIN_PASSWORD='<strong-password>'
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
