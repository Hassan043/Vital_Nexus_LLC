// Writes a secret value into an existing Key Vault (RBAC authorization required).

@description('Name of the existing Key Vault.')
param keyVaultName string

@description('Secret name (Key Vault secret identifier).')
param secretName string

@description('Secret value to store.')
@secure()
param secretValue string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: secretName
  properties: {
    value: secretValue
  }
}

output secretName string = secret.name
output secretUri string = secret.properties.secretUri
