# Database Schema Deployment

VitalNexus uses **SQL Database Projects** and **DACPAC** publishing as the only production schema mechanism. EF Core migrations are not used for Azure SQL deployment.

## Validation (CI)

Workflow: `.github/workflows/database-schema-validation.yml`

Runs on pull requests and pushes to `dev`/`main` when `database/` changes. It:

1. Fails if EF Core `Migrations` folders exist under `backend/` (production schema must be DACPAC-based).
2. Builds all SQL Database Projects to DACPAC artifacts.
3. Verifies each DACPAC is present and readable (archive integrity check).

Add this workflow as a required status check on `dev` to block schema changes that do not build.

## Deployment (manual)

Workflow: `.github/workflows/deploy-databases.yml`

Manual dispatch only. Select **dev**, **test**, or **prod**. The job runs in the matching **GitHub Environment**, which enforces approval gates when configured.

Publishes DACPACs to the environment SQL servers:

| DACPAC project | Database | Server |
|----------------|----------|--------|
| `VitalNexus.AccountBusiness.Database` | `Accounts` | core (`sql-vnx-core-<env>-*`) |
| `VitalNexus.LabMarkersData.Database` | `LabMarkersData` | core |
| `VitalNexus.PatientHealth.Database` | `PatientHealth` | phi (`sql-vnx-phi-<env>-*`) |

Infrastructure must exist (`deploy-infra.yml`) before running database deployment.

### Required GitHub Environment secrets

| Secret | Purpose |
|--------|---------|
| `AZURE_CREDENTIALS` | Azure login for deployment |
| `AZURE_SUBSCRIPTION_ID` | Target subscription |
| `SQL_ADMIN_PASSWORD` | SQL server admin password for sqlpackage publish |
| `SQL_ADMIN_LOGIN` | Optional; defaults to `vnxadmin` |

## Approval gates

Configure GitHub Environments under **Settings → Environments**:

| Environment | Recommended gate |
|-------------|------------------|
| **dev** | Optional auto-deploy; no required reviewers for schema experiments |
| **test** | Required reviewers before database publish |
| **prod** | Required reviewers + wait timer; restrict to release managers |

When `deploy-databases.yml` runs, GitHub waits for environment approval before any sqlpackage publish step executes. This is separate from branch protection — it gates **runtime deployment**, not merge.

Document reviewers and escalation in your team runbook. Do not store SQL passwords in workflow files; use environment-scoped secrets only.

## Release order

1. Merge schema changes after CI validation passes.
2. Deploy infrastructure if servers do not exist.
3. Run `deploy-databases.yml` for the target environment (after approval if required).
4. Deploy application components (`deploy-api.yml`, etc.).

See also [`VitalNexus.Database.Shared/DeploymentRules.md`](../database/VitalNexus.Database.Shared/DeploymentRules.md) and [`DEPLOYMENT_PIPELINES.md`](DEPLOYMENT_PIPELINES.md).
