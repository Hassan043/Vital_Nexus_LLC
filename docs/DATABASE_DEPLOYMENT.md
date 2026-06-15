# Database Schema Deployment

VitalNexus uses **SQL Database Projects** and **DACPAC** publishing as the only production schema mechanism. EF Core migrations are not used for Azure SQL deployment.

## CI build

Workflow: `.github/workflows/database-build.yml`

Runs on every pull request and push to `dev`/`main`. It calls `database/scripts/build-dacpacs.sh` to build `database/VitalNexus.Database.sln`, package normalized DACPAC files with a manifest, and upload the bundle for downstream deployment or inspection.

Add **CI / Database Build** as a required status check on `dev` so schema projects cannot silently break.

## DACPAC artifact generation

Workflow: `.github/workflows/generate-dacpac-artifacts.yml`

Runs on pushes to `dev`/`main` when `database/` changes and supports manual dispatch. It produces the same packaged artifact bundle as CI (`vitalnexus-dacpac-artifacts`) with a 90-day retention period and publishes the manifest to the workflow summary.

Local generation:

```bash
bash database/scripts/build-dacpacs.sh database/artifacts/dacpacs
```

The output directory contains one `.dacpac` per project plus `dacpac-manifest.json` (commit SHA, database names, and server roles).

## Validation (CI)

Workflow: `.github/workflows/database-schema-validation.yml`

Runs when `database/` or `backend/` changes. It fails if EF Core `Migrations` folders exist under `backend/` (production schema must be DACPAC-based).

Add this workflow as a required status check on `dev` to block EF migrations as a production schema path.

## Deployment (manual)

Workflow: `.github/workflows/deploy-databases.yml`

Manual dispatch only. Select **dev**, **test**, or **prod**. Optionally supply a `dacpac_workflow_run_id` from a **Generate DACPAC Artifacts** workflow run to deploy pre-built packages; leave empty to build from the checked-out source.

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
2. Confirm **Generate DACPAC Artifacts** succeeded on `dev` (or run it manually) and note the workflow run ID if deploying pre-built packages.
3. Deploy infrastructure if servers do not exist.
4. Run `deploy-databases.yml` for the target environment (after approval if required).
5. Deploy application components (`deploy-api.yml`, etc.).

See also [`VitalNexus.Database.Shared/DeploymentRules.md`](../database/VitalNexus.Database.Shared/DeploymentRules.md) and [`DEPLOYMENT_PIPELINES.md`](DEPLOYMENT_PIPELINES.md).
