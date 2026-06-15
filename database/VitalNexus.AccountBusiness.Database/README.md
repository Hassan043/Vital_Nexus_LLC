# VitalNexus.AccountBusiness.Database

Non-PHI account and business data: users, clinics, billing metadata, and tenant routing. This database lives on the **core** SQL server alongside `LabMarkersData`, never on the PHI server.

## Target deployment

| Property | Value |
|----------|-------|
| Azure SQL server | `sql-vnx-core-<env>-*` (core server) |
| Database name | `Accounts` |
| DACPAC project | `VitalNexus.AccountBusiness.Database` |

Infrastructure (`deploy-infra.yml`) creates the empty `Accounts` database. Schema is applied with `sqlpackage` from the DACPAC produced by this project.

## Deploy via GitHub Actions

Workflow: [`.github/workflows/deploy-account-business-database.yml`](../../.github/workflows/deploy-account-business-database.yml)

Manual dispatch only. Select **dev**, **test**, or **prod**. The job runs in the matching GitHub Environment (approval gates apply when configured). Optionally pass a `dacpac_workflow_run_id` from **Generate DACPAC Artifacts** to deploy a pre-built package.

Required environment secrets: `AZURE_CREDENTIALS`, `AZURE_SUBSCRIPTION_ID`, `SQL_ADMIN_PASSWORD`, and optionally `SQL_ADMIN_LOGIN` (defaults to `vnxadmin`).

## Local publish example

```bash
dotnet build VitalNexus.AccountBusiness.Database.sqlproj -c Release

bash ../scripts/publish-dacpac.sh \
  ./bin/Release/VitalNexus.AccountBusiness.Database.dacpac \
  sql-vnx-core-dev-<suffix>.database.windows.net \
  Accounts \
  vnxadmin \
  '<password>'
```

See also [`docs/DATABASE_DEPLOYMENT.md`](../../docs/DATABASE_DEPLOYMENT.md).
