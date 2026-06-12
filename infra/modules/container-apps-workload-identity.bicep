// Shared user-assigned identity for Container Apps workloads to pull images
// from Azure Container Registry without storing registry credentials.

@description('Azure region for the managed identity.')
param location string

@description('Tags applied to all resources in this module.')
param tags object = {}

@description('Name of the user-assigned managed identity.')
param identityName string

resource workloadIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

output identityName string = workloadIdentity.name
output identityId string = workloadIdentity.id
output principalId string = workloadIdentity.properties.principalId
output clientId string = workloadIdentity.properties.clientId
