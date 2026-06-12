# VitalNexus.AiAnalysis.Worker

Container-hosted .NET 8 worker for asynchronous AI analysis and related background workflows. Subscribes to Dapr pub/sub messages, processes queued work through the approved AI integration boundary, persists results, and emits sanitized telemetry.

> **Status:** Phase 1 baseline (F1.T2.3). Placeholder AI integration and persistence — production wiring arrives in later issues.

## Responsibilities

- Subscribe to `ai-analysis-queue` via Dapr pub/sub
- Validate anonymized analysis requests (no PHI on contracts)
- Invoke the approved AI integration boundary (placeholder)
- Persist results (placeholder store)
- Retry transient failures with exponential backoff
- Log and emit telemetry using sanitized fields only

## Local development

```powershell
cd backend
dotnet build src/Workers/VitalNexus.AiAnalysis.Worker/VitalNexus.AiAnalysis.Worker.csproj
dotnet run --project src/Workers/VitalNexus.AiAnalysis.Worker
```

Health check: `GET http://localhost:8080/health`

### With Dapr sidecar

```powershell
dapr run --app-id ai-analysis-worker --app-port 8080 -- dotnet run --project src/Workers/VitalNexus.AiAnalysis.Worker
```

Subscription discovery: `GET http://localhost:8080/dapr/subscribe`

## Docker

Build from the `backend/` directory:

```powershell
docker build -f src/Workers/VitalNexus.AiAnalysis.Worker/Dockerfile -t vitalnexus-ai-analysis-worker .
```

## Configuration

| Section | Purpose |
| ------- | ------- |
| `Worker` | Service identity |
| `Dapr` | Pub/sub component and topic names (must match Dapr manifests) |
| `Retry` | Max attempts and backoff |
| `ApplicationInsights` | Telemetry connection string (non-secret default empty) |

Secrets belong in environment variables, user secrets locally, or Azure Key Vault in cloud — never committed.

## Dapr subscription

| Constant | Value |
| -------- | ----- |
| Pub/sub component | `pubsub` |
| Topic | `ai-analysis-queue` |
| Route | `POST /dapr/ai-analysis-queue` |

## PHI-safe logging

Logs and telemetry include only operation IDs, anonymous patient identifiers, counts, and status — never marker names, lab values, AI prompts/responses, or clinical notes.
