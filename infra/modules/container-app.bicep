// Reusable Azure Container App module for VitalNexus services (frontend, API,
// AI worker, payment worker, and future containers).

@description('Azure region for the container app.')
param location string

@description('Tags applied to the container app.')
param tags object = {}

@description('Name of the container app (lowercase alphanumeric and hyphens).')
param containerAppName string

@description('Resource ID of the shared Container Apps managed environment.')
param managedEnvironmentId string

@description('Full container image reference (for example myacr.azurecr.io/vitalnexus-api:latest).')
param containerImage string

@description('Container CPU cores (for example 0.25).')
param cpu string = '0.25'

@description('Container memory (for example 0.5Gi).')
param memory string = '0.5Gi'

@description('Minimum replica count.')
param minReplicas int = 1

@description('Maximum replica count.')
param maxReplicas int = 1

@description('Expose HTTP ingress for the container app.')
param ingressEnabled bool = false

@description('When ingress is enabled, expose the app externally. Set false for internal-only ingress.')
param externalIngress bool = false

@description('Target port for ingress traffic.')
param targetPort int = 8080

@description('User-assigned managed identity resource ID used for ACR pull and runtime access.')
param userAssignedIdentityResourceId string

@description('ACR login server used for registry authentication via managed identity.')
param acrLoginServer string = ''

@description('Plain-text environment variables injected into the container.')
param environmentVariables array = []

@description('Container app secrets (referenced by secret-backed environment variables).')
param secrets array = []

@description('Secrets resolved from Key Vault using the user-assigned managed identity.')
param keyVaultSecrets array = []

@description('Environment variables backed by container app secrets.')
param secretEnvironmentVariables array = []

@description('Enable the Dapr sidecar for this container app.')
param daprEnabled bool = false

@description('Dapr application identifier.')
param daprAppId string = ''

@description('Port the application listens on for Dapr app-channel communication.')
param daprAppPort int = 8080

@description('Optional HTTP health probe path.')
param healthProbePath string = ''

var valueBasedSecrets = [
  for secret in secrets: {
    name: secret.name
    value: secret.value
  }
]

var keyVaultBasedSecrets = [
  for kvSecret in keyVaultSecrets: {
    name: kvSecret.name
    keyVaultUrl: kvSecret.keyVaultUrl
    identity: kvSecret.identity
  }
]

var containerAppSecrets = concat(valueBasedSecrets, keyVaultBasedSecrets)

var plainEnvironmentVariables = [for item in environmentVariables: {
  name: item.name
  value: item.value
}]

var secretBackedEnvironmentVariables = [for item in secretEnvironmentVariables: {
  name: item.name
  secretRef: item.secretName
}]

var containerEnvironmentVariables = concat(plainEnvironmentVariables, secretBackedEnvironmentVariables)

var registryConfiguration = empty(acrLoginServer)
  ? []
  : [
      {
        server: acrLoginServer
        identity: userAssignedIdentityResourceId
      }
    ]

var healthProbes = empty(healthProbePath)
  ? []
  : [
      {
        type: 'Liveness'
        httpGet: {
          path: healthProbePath
          port: targetPort
          scheme: 'HTTP'
        }
        initialDelaySeconds: 10
        periodSeconds: 30
      }
      {
        type: 'Readiness'
        httpGet: {
          path: healthProbePath
          port: targetPort
          scheme: 'HTTP'
        }
        initialDelaySeconds: 5
        periodSeconds: 10
      }
    ]

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityResourceId}': {}
    }
  }
  properties: {
    managedEnvironmentId: managedEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: ingressEnabled
        ? {
            external: externalIngress
            targetPort: targetPort
            transport: 'auto'
            allowInsecure: false
          }
        : null
      secrets: containerAppSecrets
      registries: registryConfiguration
      dapr: daprEnabled
        ? {
            enabled: true
            appId: daprAppId
            appPort: daprAppPort
          }
        : null
    }
    template: {
      containers: [
        {
          name: 'app'
          image: containerImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: containerEnvironmentVariables
          probes: healthProbes
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output containerAppName string = containerApp.name
output containerAppId string = containerApp.id
output fqdn string = ingressEnabled ? containerApp.properties.configuration.ingress.fqdn : ''
output latestRevisionName string = containerApp.properties.latestRevisionName
