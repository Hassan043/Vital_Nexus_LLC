// Key Vault for application secrets (connection strings, API keys, Stripe
// keys). RBAC authorization only — no access policies — so the API and
// Functions managed identities read secrets with least privilege and no
// credentials ever live in app settings or source control.

@description('Azure region for the Key Vault.')
param location string

@description('Tags applied to all resources in this module.')
param tags object = {}

@description('Globally unique Key Vault name (3-24 chars).')
@minLength(3)
@maxLength(24)
param keyVaultName string

@description('Object IDs of managed identities granted the Key Vault Secrets User role.')
param secretsUserPrincipalIds array = []

// Built-in role: Key Vault Secrets User.
var keyVaultSecretsUserRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e6'
)

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
  }
}

resource secretsUserAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in secretsUserPrincipalIds: {
    name: guid(keyVault.id, principalId, keyVaultSecretsUserRoleId)
    scope: keyVault
    properties: {
      roleDefinitionId: keyVaultSecretsUserRoleId
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
