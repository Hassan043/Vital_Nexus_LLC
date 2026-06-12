# Container Builds

Dockerfiles for VitalNexus application components targeting Azure Container Apps and Azure Container Registry (ACR). Deployment to ACA is handled in a later issue.

## Image names (ACR)

Replace `{acr}` with your registry login server (for example `vnxdev.azurecr.io`).

| Component | Local tag | ACR repository | Example ACR tag |
| --------- | --------- | -------------- | --------------- |
| Frontend | `vitalnexus-frontend:local` | `vitalnexus-frontend` | `{acr}/vitalnexus-frontend:{version}` |
| API | `vitalnexus-api:local` | `vitalnexus-api` | `{acr}/vitalnexus-api:{version}` |
| AI analysis worker | `vitalnexus-ai-analysis-worker:local` | `vitalnexus-ai-analysis-worker` | `{acr}/vitalnexus-ai-analysis-worker:{version}` |

Recommended CI tags: `{version}` (git SHA or semver), `latest` for dev only.

## Build contexts

| Dockerfile | Build context | Port |
| ---------- | ------------- | ---- |
| `frontend/Dockerfile` | `frontend/` | 8080 (nginx) |
| `backend/src/Api/VitalNexus.Api/Dockerfile` | `backend/` | 8080 |
| `backend/src/Workers/VitalNexus.AiAnalysis.Worker/Dockerfile` | `backend/` | 8080 |

## Local build commands

### Frontend

```powershell
cd frontend
docker build -f Dockerfile -t vitalnexus-frontend:local .
docker run --rm -p 8080:8080 vitalnexus-frontend:local
```

### API

```powershell
cd backend
docker build -f src/Api/VitalNexus.Api/Dockerfile -t vitalnexus-api:local .
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development vitalnexus-api:local
```

Health check: `GET http://localhost:8080/health`

### AI analysis worker

Requires the `VitalNexus.AiAnalysis.Worker` project (F1.T2.3).

```powershell
cd backend
docker build -f src/Workers/VitalNexus.AiAnalysis.Worker/Dockerfile -t vitalnexus-ai-analysis-worker:local .
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development vitalnexus-ai-analysis-worker:local
```

Run with Dapr locally (optional):

```powershell
dapr run --app-id ai-analysis-worker --app-port 8080 -- docker run --rm -p 8080:8080 vitalnexus-ai-analysis-worker:local
```

## Push to ACR (manual)

```powershell
az acr login --name {acrName}
docker tag vitalnexus-frontend:local {acr}/vitalnexus-frontend:{version}
docker tag vitalnexus-api:local {acr}/vitalnexus-api:{version}
docker tag vitalnexus-ai-analysis-worker:local {acr}/vitalnexus-ai-analysis-worker:{version}
docker push {acr}/vitalnexus-frontend:{version}
docker push {acr}/vitalnexus-api:{version}
docker push {acr}/vitalnexus-ai-analysis-worker:{version}
```

## Notes

- `.dockerignore` files exclude `node_modules`, build artifacts, tests, and unrelated projects from each context.
- Backend builds use `backend/.dockerignore` at the shared build context root.
- The frontend image serves Vite-built static assets via nginx with SPA fallback routing.
- API and worker images use the .NET 8 ASP.NET runtime and listen on port 8080.
- The worker container runs as a long-lived `dotnet` process suitable for Dapr sidecar integration.
