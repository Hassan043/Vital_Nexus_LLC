// Provisions a dedicated Microsoft Entra / Azure AD B2C tenant in the target resource group.
// Tenant names must be globally unique and alphanumeric (no hyphens in the domain prefix).
// See infra/identity/README.md for post-deploy steps (user flows, app registrations).

@description('Azure AD B2C tenant domain prefix without the .onmicrosoft.com suffix.')
@minLength(3)
@maxLength(24)
param tenantDomainPrefix string

@description('Human-readable tenant display name shown in the Azure portal.')
param displayName string

@description('ISO 3166-1 alpha-2 country code used when creating the tenant.')
param countryCode string = 'US'

@description('Data residency location for the B2C tenant (not an Azure region such as eastus).')
@allowed([
  'United States'
  'Europe'
  'Asia Pacific'
  'Australia'
  'Brazil'
  'Canada'
  'Japan'
  'Korea'
  'India'
])
param dataResidencyLocation string = 'United States'

@description('SKU name for the B2C tenant billing tier identifier (not a monetary value).')
param skuName string = 'Standard'

@description('SKU tier for the B2C tenant.')
param skuTier string = 'A0'

@description('Tags applied to the B2C tenant resource.')
param tags object = {}

var tenantDomain = '${tenantDomainPrefix}.onmicrosoft.com'

resource b2cTenant 'Microsoft.AzureActiveDirectory/b2cDirectories@2023-05-17-preview' = {
  name: tenantDomain
  location: dataResidencyLocation
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    createTenantProperties: {
      countryCode: countryCode
      displayName: displayName
    }
  }
}

output tenantDomain string = tenantDomain
output tenantResourceId string = b2cTenant.id
output loginBaseUrl string = 'https://${tenantDomainPrefix}.b2clogin.com'
