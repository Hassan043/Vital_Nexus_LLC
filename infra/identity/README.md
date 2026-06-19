# Microsoft Entra External ID — External Tenant (F3.T1.1)

VitalNexus uses **Microsoft Entra External ID** (customer identity platform; ARM type `Microsoft.AzureActiveDirectory/b2cDirectories`) for authentication. The platform **never stores usernames, passwords, or MFA secrets** in the Accounts database. Entra External ID issues validated tokens; the backend trusts those tokens (see F3.T2.x).

Each deployment environment (`dev`, `test`, `prod`) owns a **dedicated external tenant**. Do not share tenants across environments.

## What this folder deploys

| Resource | Purpose |
|----------|---------|
| `Microsoft.AzureActiveDirectory/b2cDirectories` | Environment-specific external tenant |
| Key Vault secrets (optional) | `b2c-tenant-domain` |

User flows, MFA policies, and additional app configuration are configured in follow-on issues (F3.T1.5+). App registrations and user flows:

- **F3.T1.2** — React frontend SPA — [`spa-app/README.md`](spa-app/README.md)
- **F3.T1.3** — Backend API and scopes — [`api-app/README.md`](api-app/README.md)
- **F3.T1.4** — Customer sign-up/sign-in user flow — [`user-flows/README.md`](user-flows/README.md)

## Prerequisites

1. Azure subscription with permission to create B2C tenants.
2. Resource group `rg-vitalnexus-<env>` (created by **Deploy Infrastructure** or manually).
3. Globally unique tenant domain prefix (alphanumeric only — **no hyphens**), e.g. `vitalnexusdev` → `vitalnexusdev.onmicrosoft.com`.
4. If the prefix is taken, edit `main.<env>.bicepparam` before deploying.

## Deploy (GitHub Actions)

Workflow: [`.github/workflows/deploy-identity.yml`](../../.github/workflows/deploy-identity.yml)

Manual dispatch → select **dev**, **test**, or **prod**.

## Deploy (local CLI)

```powershell
$env:SQL_ADMIN_PASSWORD = '<unused-for-identity-but-required-if-using-same-shell>'

az group show --name rg-vitalnexus-dev

az deployment group create `
  -g rg-vitalnexus-dev `
  -f infra/identity/main.bicep `
  -p infra/identity/main.dev.bicepparam
```

To also write tenant metadata into the environment Key Vault after core infra exists:

```powershell
az deployment group create `
  -g rg-vitalnexus-dev `
  -f infra/identity/main.bicep `
  -p infra/identity/main.dev.bicepparam `
  -p keyVaultName=kv-vnx-dev-<suffix>
```

## Verify tenant

```powershell
.\scripts\verify-b2c-tenant.ps1 -TenantDomain vitalnexusdev.onmicrosoft.com
```

Or pass `-TenantId` from deployment outputs.

## Deployment outputs

| Output | Example | Use |
|--------|---------|-----|
| `tenantDomain` | `vitalnexusdev.onmicrosoft.com` | Authority / issuer configuration |
| `tenantResourceId` | ARM resource ID | Support / portal deep links |
| `loginBaseUrl` | `https://vitalnexusdev.b2clogin.com` | Frontend MSAL authority base |

Resolve the **tenant GUID** after deploy (not available as a Bicep output property):

```powershell
az rest --method get `
  --url "https://management.azure.com/subscriptions/<subscription-id>/resourceGroups/rg-vitalnexus-dev/providers/Microsoft.AzureActiveDirectory/b2cDirectories/vitalnexusdev.onmicrosoft.com?api-version=2023-05-17-preview" `
  --query "properties.tenantId" -o tsv
```

## Post-deploy manual steps (portal)

After the tenant resource exists:

1. Open **Microsoft Entra External ID** → select the new tenant.
2. Confirm **Tenant properties** (display name, country).
3. Note the **Tenant ID** from Azure portal or the `az rest` command above.
4. Proceed to **F3.T1.2** (register React frontend SPA app) — [`spa-app/README.md`](spa-app/README.md).
5. Proceed to **F3.T1.3** (register backend API and expose scopes) — [`api-app/README.md`](api-app/README.md).
6. Proceed to **F3.T1.4** (customer sign-up/sign-in user flow) — [`user-flows/README.md`](user-flows/README.md).

## Limitations

- Bicep can **create** a tenant but cannot **update** an existing tenant with the same domain name.
- If deployment fails because the tenant already exists, either use the existing tenant or choose a new `tenantDomainPrefix`.
- User flows and MFA policies are not provisioned by this template.

## Related docs

- [`infra/environments/secrets-and-databases.md`](../environments/secrets-and-databases.md) — per-environment isolation
- [Set up sign-in for a Microsoft Entra organization - Azure AD B2C](https://learn.microsoft.com/en-us/azure/active-directory-b2c/identity-provider-azure-ad-single-tenant)
