using './main.bicep'

param environmentName = 'dev'
param tenantDomainPrefix = 'vitalnexusdev'
param displayName = 'VitalNexus Dev'
param countryCode = 'US'
param dataResidencyLocation = 'United States'

// Optional: set after core infra deploy resolves the environment Key Vault name, e.g. kv-vnx-dev-xxxxxxx
param keyVaultName = ''
