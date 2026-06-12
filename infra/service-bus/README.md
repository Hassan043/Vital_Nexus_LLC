# Azure Service Bus — Deployed Pub/Sub Broker

Azure Service Bus is the Dapr pub/sub broker in deployed environments. Application code uses Dapr with the shared component name `pubsub` — never the Service Bus SDK directly.

## Namespace

| Environment | SKU | Notes |
| ----------- | --- | ----- |
| dev | Standard | Lowest tier that supports topics; shorter default message TTL (`P7D`) |
| prod | Standard | Same tier baseline; longer default message TTL (`P14D`) |

Namespace naming pattern: `sb-{prefix}-{environment}-{suffix}` (for example `sb-vnx-dev-abc12345`).

## Secure access

| Mechanism | Purpose |
| --------- | ------- |
| Key Vault secret `servicebus-connection-string` | Dapr `pubsub` component connection (ACA-managed secret at deploy time) |
| Azure Service Bus Data Sender role | Container Apps workload identity can publish via passwordless paths |
| Azure Service Bus Data Receiver role | Container Apps workload identity can subscribe via passwordless paths |

The deployer identity requires permission to write Key Vault secrets during infrastructure deployment.

## Background workflow topics

| Topic | Purpose | Typical publisher | Typical subscriber |
| ----- | ------- | ----------------- | ------------------ |
| `ai-analysis-queue` | AI analysis requested | `vitalnexus-api` | `ai-analysis-worker` |
| `ai-analysis-completed` | AI analysis completed | `ai-analysis-worker` | `vitalnexus-api`, future notification worker |
| `ai-analysis-failed` | AI analysis failed | `ai-analysis-worker` | `vitalnexus-api`, future notification worker |
| `payment-event-received` | Stripe/payment webhook follow-up | `vitalnexus-api` | future payment worker |
| `notification-requested` | Email/in-app notification dispatch | `vitalnexus-api`, workers | future notification worker |
| `export-requested` | Patient/export package generation | `vitalnexus-api` | future export worker |
| `retention-scan-requested` | Archive/retention background scan | scheduler/API | future retention worker |

Topics are provisioned up front so later worker containers can subscribe without infrastructure shape changes. Only `ai-analysis-queue` is wired in application code today; the others are reserved for upcoming phases.

## Container app usage today

| Container app | Dapr app ID | Publish topics | Subscribe topics |
| ------------- | ----------- | -------------- | ---------------- |
| API | `vitalnexus-api` | `ai-analysis-queue` (planned) | — |
| AI analysis worker | `ai-analysis-worker` | `ai-analysis-completed`, `ai-analysis-failed` (planned) | `ai-analysis-queue` |
| Frontend | — | — | — |

## Infrastructure outputs

The `service-bus` module exposes outputs consumed by `main.bicep` and the Dapr pub/sub component:

- `namespaceName`
- `namespaceEndpoint`
- `topicNames`
- `aiAnalysisRequestedTopicName`
- `primaryConnectionString` (secure — stored in Key Vault, not committed)

## Related docs

- Dapr component wiring: `infra/dapr/README.md`
- Bicep module: `infra/modules/service-bus.bicep`
