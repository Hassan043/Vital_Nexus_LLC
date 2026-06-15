# Application Deployment Pipelines

Manual GitHub Actions workflows deploy VitalNexus application components to **dev**, **test**, or **prod**. Each workflow targets the matching GitHub Environment and Azure resource group (`rg-vitalnexus-<env>`).

Infrastructure must be deployed first (`.github/workflows/deploy-infra.yml`).

## Pipelines

| Workflow | Component | Target |
|----------|-----------|--------|
| `deploy-frontend.yml` | React frontend (nginx container) | Azure Container App |
| `deploy-api.yml` | ASP.NET Core API | Azure Container App |
| `deploy-functions.yml` | Azure Functions (isolated worker) | Azure Function App |

All three are **workflow_dispatch** only — select the environment when running the workflow.

## Container deployments (frontend and API)

1. Resolve the environment ACR and Container App from the resource group.
2. Build the Docker image from the repo Dockerfile.
3. Push to ACR as `{acr}.azurecr.io/{repository}:{tag}`.
4. Update the Container App image with `az containerapp update`.

Default image tag is the 12-character commit SHA. Override with the optional `image_tag` input.

| Component | ACR repository | Dockerfile |
|-----------|----------------|------------|
| Frontend | `vitalnexus-frontend` | `frontend/Dockerfile` |
| API | `vitalnexus-api` | `backend/src/Api/VitalNexus.Api/Dockerfile` |

See also [`CONTAINER_BUILDS.md`](CONTAINER_BUILDS.md) for local build commands.

## Functions deployment

1. `dotnet publish` the `VitalNexus.Functions` project.
2. Deploy the publish folder to the environment Function App via `Azure/functions-action`.

The Function App name is resolved from:

1. GitHub Environment secret `AZURE_FUNCTION_APP_NAME` (preferred), or
2. Auto-discovery of `func-vnx-<env>*` in the resource group.

If no Function App exists in the environment, set `AZURE_FUNCTION_APP_NAME` after provisioning one, or skip this pipeline until Functions hosting is added to infrastructure.

## Required GitHub Environment secrets

Configure separately for **dev**, **test**, and **prod**:

| Secret | Used by |
|--------|---------|
| `AZURE_CREDENTIALS` | All deploy workflows |
| `AZURE_SUBSCRIPTION_ID` | All deploy workflows |
| `AZURE_FUNCTION_APP_NAME` | Functions deploy (optional if auto-discovery works) |

The deploy service principal needs:

- **ACR:** `AcrPush` on the environment registry
- **Container Apps:** `Contributor` or Container Apps Contributor on the resource group
- **Function App:** Website Contributor on the Function App (Functions pipeline only)

## Typical release order

1. Deploy infrastructure (`deploy-infra.yml`) if not already provisioned.
2. Deploy databases (`deploy-databases.yml`) after CI schema validation passes and environment approval (if configured). Deploy Account Business alone via `deploy-account-business-database.yml` or Patient Health via `deploy-patient-health-database.yml` when rolling out schemas independently.
3. Deploy API (`deploy-api.yml`).
4. Deploy frontend (`deploy-frontend.yml`).
5. Deploy Functions (`deploy-functions.yml`) when a Function App is available.

Database schema validation runs automatically on PRs via `database-schema-validation.yml`. SQL Database Projects are built on every PR via `database-build.yml`. Packaged DACPAC artifacts are generated on merge via `generate-dacpac-artifacts.yml`. Schema drift against live Azure SQL is checked manually via `database-schema-drift.yml`. See [`DATABASE_DEPLOYMENT.md`](DATABASE_DEPLOYMENT.md).
