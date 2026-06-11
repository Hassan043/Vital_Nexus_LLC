// General-purpose application storage account (export packages, attachments)
// with optional blob containers. Distinct from the Functions runtime storage
// account so application data never mixes with runtime state.

@description('Azure region for the storage account.')
param location string

@description('Tags applied to all resources in this module.')
param tags object = {}

@description('Globally unique storage account name (3-24 chars, lowercase alphanumeric).')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('Blob containers to create in the account.')
param containerNames array = []

@description('Days deleted blobs remain recoverable (supports the no-automatic-deletion principle).')
param blobSoftDeleteRetentionDays int = 30

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: blobSoftDeleteRetentionDays
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: blobSoftDeleteRetentionDays
    }
  }
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [
  for containerName in containerNames: {
    parent: blobService
    name: containerName
    properties: {
      publicAccess: 'None'
    }
  }
]

output storageAccountName string = storageAccount.name
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
