# Secrets and Databases per Environment

Each VitalNexus deployment environment (`dev`, `test`, `prod`) owns its own Azure SQL servers, databases, Key Vault, and Service Bus namespace. Nothing is shared across environments at the data or secrets layer.

## Database isolation

Every environment deploys into its own resource group (`rg-vitalnexus-<env>`) with dedicated SQL logical servers:

| Server | Databases | PHI |
|--------|-----------|-----|
| `sql-vnx-core-<env>-*` | `Accounts`, `LabMarkersData` | No |
| `sql-vnx-phi-<env>-*` | `PatientHealth` | Yes |

Server names include the environment suffix and resource group scope, so dev databases never share servers with test or prod. Schema is applied per environment via DACPAC from the SQL Database Projects.

## Key Vault secrets per environment

Each environment has its own Key Vault (`kv-vnx-<env>-*`). The deployer writes runtime secrets during infrastructure deployment:

| Secret name | Purpose |
|-------------|---------|
| `accounts-db-connection-string` | Accounts database connection |
| `lab-markers-data-db-connection-string` | LabMarkersData reference database connection |
| `patient-health-db-connection-string` | PatientHealth (PHI) database connection |
| `servicebus-connection-string` | Environment-specific Service Bus namespace |

Connection strings are built at deploy time from the environment's SQL servers and credentials. They are never committed to source control.

The API Container App reads database connection strings from its environment Key Vault using the workload managed identity (`ConnectionStrings__Accounts`, `ConnectionStrings__LabMarkersData`, `ConnectionStrings__PatientHealth`). The vault URI is exposed as `KeyVault__VaultUri`.

## GitHub Environment secrets

Configure **separate secrets for each GitHub Environment** (`dev`, `test`, `prod`). Do not reuse production SQL passwords or credentials in dev or test.

| Secret | Scope | Notes |
|--------|-------|-------|
| `SQL_ADMIN_PASSWORD` | Per environment | Unique strong password for each environment's SQL servers |
| `SQL_ADMIN_LOGIN` | Per environment (optional) | Defaults to `vnxadmin` in Bicepparam if omitted |
| `AZURE_CREDENTIALS` | Per environment or repo | Service principal JSON for deployment |
| `AZURE_SUBSCRIPTION_ID` | Per environment or repo | Target subscription |

The deploy workflow (`.github/workflows/deploy-infra.yml`) runs in the selected GitHub Environment and passes `SQL_ADMIN_LOGIN` / `SQL_ADMIN_PASSWORD` into Bicep at deploy time.

## Verification

After deploying an environment, confirm isolation:

1. Key Vault contains the four secrets listed above and no secrets from other environments.
2. SQL servers in the resource group match the `<env>` suffix.
3. API Container App environment variables reference connection strings via Key Vault secret refs, not plain-text values.

See [`README.md`](README.md) for environment overview and deploy steps.
