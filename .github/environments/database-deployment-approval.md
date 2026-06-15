# Production database deployment approval

Production schema changes must pass **two** gates before `sqlpackage` publishes to Azure SQL in prod:

1. **Workflow confirmation** — the deploy workflow requires `confirm_production_deploy=approve-prod-db` when `environment=prod`.
2. **GitHub Environment approval** — the deploy job runs in the `prod` environment and waits for configured reviewers before any Azure steps execute.

Non-production environments (`dev`, `test`) skip the typed confirmation. Configure optional reviewers on `test` and required reviewers on `prod` in GitHub repository settings.

## Configure the prod environment

In **Settings → Environments → prod**:

| Setting | Recommended value |
|---------|-------------------|
| Required reviewers | At least one release manager or DBA; prefer two for PHI-adjacent schema |
| Wait timer | 5 minutes (allows cancellation after notification) |
| Deployment branches | Selected branches → `main` only |
| Environment secrets | Prod-only `AZURE_CREDENTIALS`, `AZURE_SUBSCRIPTION_ID`, `SQL_ADMIN_PASSWORD` |

Repeat similar reviewer rules for **test** if pre-production schema promotion should also be gated.

## Production deploy procedure

1. Merge schema changes to `main` after CI build and validation pass on `dev`.
2. Deploy and verify schema in **dev**, then **test**.
3. Run **Database Schema Drift Detection** against test and confirm no drift (or expected drift is understood).
4. From the **main** branch, dispatch the database deploy workflow with:
   - `environment`: `prod`
   - `confirm_production_deploy`: `approve-prod-db`
5. Wait for GitHub Environment reviewers to approve the pending deployment job.
6. After publish completes, run drift detection against prod.

Workflows that enforce this gate:

- `.github/workflows/deploy-databases.yml`
- `.github/workflows/deploy-account-business-database.yml`
- `.github/workflows/deploy-patient-health-database.yml`

The gate is implemented by `.github/actions/database-prod-approval-gate` and the `environment: prod` job binding on the deploy job.
