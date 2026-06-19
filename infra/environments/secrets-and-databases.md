# Secrets and Databases per Environment

Each VitalNexus deployment environment (`dev`, `test`, `prod`) owns its own Azure SQL servers, databases, Key Vault, Service Bus namespace, and **Microsoft Entra External ID external tenant**. Nothing is shared across environments at the data, secrets, or identity layer.

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

## Identity provider (Entra External ID) per environment

Each environment has its own **Microsoft Entra External ID external tenant** deployed from [`infra/identity/main.bicep`](../identity/main.bicep). Tenant domain prefixes are defined in `infra/identity/main.<env>.bicepparam` (defaults: `vitalnexusdev`, `vitalnexustest`, `vitalnexusprod`).

| Key Vault secret (optional) | Purpose |
|-----------------------------|---------|
| `b2c-tenant-domain` | Tenant domain (`<prefix>.onmicrosoft.com`) |
| `b2c-spa-client-id` | React frontend SPA application (client) ID (F3.T1.2) |

The B2C tenant GUID is not returned by the Bicep resource type. After deploy, resolve it with:

```powershell
az rest --method get `
  --url "https://management.azure.com/subscriptions/<subscription-id>/resourceGroups/rg-vitalnexus-dev/providers/Microsoft.AzureActiveDirectory/b2cDirectories/vitalnexusdev.onmicrosoft.com?api-version=2023-05-17-preview" `
  --query "properties.tenantId" -o tsv
```

Store the resolved tenant ID in Key Vault manually or in F3.T1.5 application configuration.

Deploy with [`.github/workflows/deploy-identity.yml`](../../.github/workflows/deploy-identity.yml). Pass the environment Key Vault name to persist tenant metadata after core infra exists.

Register the React frontend SPA app with [`.github/workflows/configure-b2c-spa-app.yml`](../../.github/workflows/configure-b2c-spa-app.yml) or [`scripts/register-b2c-spa-app.ps1`](../../scripts/register-b2c-spa-app.ps1). See [`infra/identity/spa-app/README.md`](../identity/spa-app/README.md).

**Never store user passwords, MFA secrets, or refresh tokens in VitalNexus databases.** Authentication is delegated to Microsoft Entra External ID.

## GitHub Environment secrets

Configure **separate secrets for each GitHub Environment** (`dev`, `test`, `prod`). Do not reuse production SQL passwords or credentials in dev or test.

| Secret | Scope | Notes |
|--------|-------|-------|
| `SQL_ADMIN_PASSWORD` | Per environment | Unique strong password for each environment's SQL servers |
| `SQL_ADMIN_LOGIN` | Per environment (optional) | Defaults to `vnxadmin` in Bicepparam if omitted |
| `AZURE_CREDENTIALS` | Per environment or repo | Service principal JSON for deployment |
| `AZURE_SUBSCRIPTION_ID` | Per environment or repo | Target subscription |
| `B2C_TENANT_ID` | Per environment | Entra External ID tenant GUID |
| `B2C_MANAGEMENT_CLIENT_ID` | Per environment | Graph management app client ID |
| `B2C_MANAGEMENT_CLIENT_SECRET` | Per environment | Graph management app client secret |
| `B2C_SPA_CLIENT_ID` | Per environment | React frontend SPA client ID (output of F3.T1.2) |

The deploy workflow (`.github/workflows/deploy-infra.yml`) runs in the selected GitHub Environment and passes `SQL_ADMIN_LOGIN` / `SQL_ADMIN_PASSWORD` into Bicep at deploy time.

## Verification

After deploying an environment, confirm isolation:

1. Key Vault contains the four database/Service Bus secrets listed above and no secrets from other environments.
2. SQL servers in the resource group match the `<env>` suffix.
3. API Container App environment variables reference connection strings via Key Vault secret refs, not plain-text values.
4. Entra External ID tenant domain and tenant ID match the environment's `infra/identity/main.<env>.bicepparam` deployment outputs.
5. SPA app registration exists with required redirect URIs (`scripts/verify-b2c-spa-app.ps1`).

See [`README.md`](README.md) for environment overview and deploy steps.
