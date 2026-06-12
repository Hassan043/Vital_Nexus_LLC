# Dapr Pub/Sub Configuration

VitalNexus uses Dapr pub/sub so services communicate through a broker without hard-coding SDK-specific messaging code. Application code references the shared component name `pubsub`; only the Dapr component configuration changes between local and Azure environments.

## Component name

| Setting | Value |
| ------- | ----- |
| Dapr pub/sub component | `pubsub` |
| Initial AI analysis topic | `ai-analysis-queue` |

Application code (API publisher and AI worker subscriber) must use these names via configuration — never embed Azure Service Bus or RabbitMQ SDK calls directly.

## Local development (RabbitMQ)

Local development uses RabbitMQ through the Dapr RabbitMQ pub/sub component.

1. Start RabbitMQ locally (Docker example):

```powershell
docker run -d --name vitalnexus-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

2. Copy the local component manifest:

```powershell
mkdir $HOME\.dapr\components -Force
Copy-Item infra/dapr/components/local/pubsub.yaml $HOME\.dapr\components\pubsub.yaml
```

3. Run services with Dapr sidecars using app IDs that match component scopes:

```powershell
# API
dapr run --app-id vitalnexus-api --app-port 8080 --components-path $HOME\.dapr\components -- dotnet run --project backend/src/Api/VitalNexus.Api

# AI analysis worker
dapr run --app-id ai-analysis-worker --app-port 8080 --components-path $HOME\.dapr\components -- dotnet run --project backend/src/Workers/VitalNexus.AiAnalysis.Worker
```

Local component file: `infra/dapr/components/local/pubsub.yaml`

## Azure deployment (Service Bus)

Deployed environments provision:

- Azure Service Bus namespace (Standard tier — required for topics)
- Initial topic: `ai-analysis-queue`
- Key Vault secret: `servicebus-connection-string`
- ACA Dapr component: `pubsub` (`pubsub.azure.servicebus.topics`)

Bicep modules:

| Module | Purpose |
| ------ | ------- |
| `infra/modules/service-bus.bicep` | Namespace + initial topic |
| `infra/modules/key-vault-secret.bicep` | Stores broker connection string |
| `infra/modules/dapr-pubsub-component.bicep` | ACA Dapr pub/sub component |

### Scoped container apps

The `pubsub` component is scoped to Dapr app IDs:

- `vitalnexus-api` — publishes queue messages
- `ai-analysis-worker` — subscribes to queue messages

The frontend container app does not receive the pub/sub component scope.

### Secrets

The Service Bus connection string is:

1. Stored in Key Vault as `servicebus-connection-string`
2. Injected into the ACA Dapr component secret store at deploy time (not committed to source control)

Set no broker credentials in application `appsettings.json`. Use Dapr APIs only.

## Switching brokers

To change brokers, update Dapr component configuration only:

| Environment | Broker | Component type |
| ----------- | ------ | -------------- |
| Local | RabbitMQ | `pubsub.rabbitmq` |
| Azure | Service Bus Topics | `pubsub.azure.servicebus.topics` |

Application code remains unchanged as long as it uses the Dapr `pubsub` component name and shared topic names.

## Deploy (Azure)

Service Bus and the Dapr component deploy with the main infrastructure entry point:

```powershell
$env:SQL_ADMIN_PASSWORD = '<strong-password>'
az deployment group create -g rg-vitalnexus-dev -f infra/main.bicep -p infra/main.dev.bicepparam
```

Outputs include `serviceBusNamespaceName`, `daprPubSubComponentName`, and `keyVaultUri` (for secret retrieval).
