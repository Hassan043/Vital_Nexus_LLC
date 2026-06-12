// Azure Container Registry for VitalNexus container images (frontend, API,
// AI worker, and future worker services). RBAC-only — admin user disabled.

@description('Azure region for the container registry.')
param location string

@description('Tags applied to all resources in this module.')
param tags object = {}

@description('Globally unique ACR name (5-50 alphanumeric characters).')
@minLength(5)
@maxLength(50)
param acrName string

@description('ACR SKU name (capacity tier identifier, not a monetary value).')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Basic'

@description('Object IDs granted AcrPull on this registry.')
param acrPullPrincipalIds array = []

var acrPullRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe829d630c'
)

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'disabled'
      }
      retentionPolicy: {
        days: 7
        status: 'disabled'
      }
    }
  }
}

resource acrPullAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in acrPullPrincipalIds: {
    name: guid(registry.id, principalId, acrPullRoleId)
    scope: registry
    properties: {
      roleDefinitionId: acrPullRoleId
      principalId: principalId
      principalType: 'ServicePrincipal'
    }
  }
]

output acrName string = registry.name
output acrLoginServer string = registry.properties.loginServer
output acrId string = registry.id
