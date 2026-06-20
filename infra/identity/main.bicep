// VitalNexus identity infrastructure entry point (Microsoft Entra / Azure AD B2C).
// Deploy into rg-vitalnexus-<env> after the core environment exists, or alongside it.
// Each environment receives its own B2C tenant — never share tenants across dev, test, or prod.

@description('Deployment environment (dev, test, or prod).')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string

@description('Azure AD B2C tenant domain prefix without .onmicrosoft.com (alphanumeric, globally unique).')
@minLength(3)
@maxLength(24)
param tenantDomainPrefix string

@description('Display name for the B2C tenant in the Azure portal.')
param displayName string

@description('ISO 3166-1 alpha-2 country code for tenant creation.')
param countryCode string = 'US'

@description('Data residency location for the B2C tenant.')
param dataResidencyLocation string = 'United States'

@description('Optional. When set, tenant metadata is written to this existing Key Vault.')
param keyVaultName string = ''

var tags = {
  product: 'VitalNexus'
  environment: environmentName
  managedBy: 'bicep'
  component: 'identity'
}

module b2cTenant 'modules/b2c-tenant.bicep' = {
  name: 'b2c-tenant'
  params: {
    tenantDomainPrefix: tenantDomainPrefix
    displayName: displayName
    countryCode: countryCode
    dataResidencyLocation: dataResidencyLocation
    tags: tags
  }
}

module tenantDomainSecret 'modules/key-vault-tenant-secret.bicep' = if (!empty(keyVaultName)) {
  name: 'b2c-tenant-domain-secret'
  params: {
    keyVaultName: keyVaultName
    secretName: 'b2c-tenant-domain'
    secretValue: b2cTenant.outputs.tenantDomain
  }
}

output environmentName string = environmentName
output tenantDomain string = b2cTenant.outputs.tenantDomain
output tenantResourceId string = b2cTenant.outputs.tenantResourceId
output loginBaseUrl string = b2cTenant.outputs.loginBaseUrl
output keyVaultTenantDomainSecretName string = !empty(keyVaultName) ? tenantDomainSecret!.outputs.secretName : ''
