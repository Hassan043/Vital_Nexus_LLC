using './main.bicep'

param environmentName = 'test'
param namePrefix = 'vnx'
param sqlAdministratorLogin = 'vnxadmin'
param acrSkuName = 'Standard'
param sqlDatabaseSkuName = 'S0'
param logAnalyticsRetentionInDays = 60
param containerImageTag = 'latest'
param deployPaymentWorker = false
param serviceBusSkuName = 'Standard'
param daprPubSubComponentName = 'pubsub'
param aiAnalysisTopicName = 'ai-analysis-queue'

// SECURITY: never commit a real password. Injected at deploy time (empty
// password default so the repo holds no secret). Set before deploying:
//   PowerShell:  $env:SQL_ADMIN_LOGIN = 'vnxadmin'
//                $env:SQL_ADMIN_PASSWORD = '<strong-password>'
//   bash:        export SQL_ADMIN_LOGIN='vnxadmin'
//                export SQL_ADMIN_PASSWORD='<strong-password>'
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
