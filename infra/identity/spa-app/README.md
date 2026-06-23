# Entra External ID SPA app registration — React frontend (F3.T1.2)

Registers the VitalNexus **React single-page application** in each environment's Microsoft Entra External ID tenant. The frontend is a **public client** — no client secret.

Authentication uses the authorization code flow with PKCE (MSAL). Implicit grant stays disabled.

## App registration settings

| Setting | Value |
|---------|-------|
| Platform | Single-page application |
| Redirect URI (local) | `http://localhost:5173/` |
| Post-logout redirect (local) | `http://localhost:5173/sign-in` (registered automatically by `register-b2c-spa-app.ps1`) |
| Redirect URI (deployed) | `https://<frontend-container-app-fqdn>/` |
| Client secret | None (public SPA) |
| Implicit grant | Disabled |

Record the **Application (client) ID** as `B2C_SPA_CLIENT_ID` (GitHub Environment secret) and `b2c-spa-client-id` (Key Vault).

## Per-environment redirect URIs

Configured in [`environments.json`](environments.json):

| Environment | Static URIs | Deployed URI |
|-------------|-------------|--------------|
| dev | `http://localhost:5173/` | Resolved from `ca-vnx-frontend-dev-*` Container App when deployed |
| test | `http://localhost:5173/` | Resolved from `ca-vnx-frontend-test-*` Container App when deployed |
| prod | `https://app.vitalnexus.com/` | Fixed production host |

Update `prod.redirectUris` before the first production deploy if the public hostname differs.

## Prerequisites

1. **F3.T1.1** external tenant deployed for the target environment.
2. **Management app** in the same B2C tenant with `Application.ReadWrite.All` (Microsoft Graph application permission, admin consented). Store credentials in GitHub Environment secrets:
   - `B2C_TENANT_ID`
   - `B2C_MANAGEMENT_CLIENT_ID`
   - `B2C_MANAGEMENT_CLIENT_SECRET`
3. Resolve `B2C_TENANT_ID` after tenant deploy (see [`../README.md`](../README.md)).

### Create the management app (one-time per tenant, portal)

1. **Microsoft Entra External ID** → target tenant → **App registrations** → **New registration**
2. Name: `VitalNexus B2C Management`
3. Supported account types: **Accounts in this organizational directory only**
4. No redirect URI required.
5. **Certificates & secrets** → new client secret → store as `B2C_MANAGEMENT_CLIENT_SECRET`.
6. **API permissions** → **Microsoft Graph** → **Application permissions** → `Application.ReadWrite.All` → **Grant admin consent**.

## Register (automated)

```powershell
$env:B2C_TENANT_ID = '<tenant-guid>'
$env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
$env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'

.\scripts\register-b2c-spa-app.ps1 -Environment dev

# Optional: persist client ID to Key Vault after core infra exists
.\scripts\register-b2c-spa-app.ps1 -Environment dev -KeyVaultName kv-vnx-dev-<suffix>
```

## Register (GitHub Actions)

Workflow: [`.github/workflows/configure-b2c-spa-app.yml`](../../../.github/workflows/configure-b2c-spa-app.yml)

Manual dispatch → select environment → optionally pass Key Vault name to store `b2c-spa-client-id`.

## Verify

```powershell
$env:B2C_TENANT_ID = '<tenant-guid>'
$env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
$env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'

.\scripts\verify-b2c-spa-app.ps1 -Environment dev
```

## Portal steps (manual alternative)

1. **Microsoft Entra External ID** → B2C tenant → **App registrations** → **New registration**
2. Name: per `environments.json` `displayName`
3. Supported account types: **Accounts in this organizational directory only**
4. Redirect URI: **Single-page application** → `http://localhost:5173/`
5. After create: **Authentication** → add deployed frontend URIs from `environments.json`
6. Copy **Application (client) ID** → `B2C_SPA_CLIENT_ID` and Key Vault `b2c-spa-client-id`

## Related

- [`../README.md`](../README.md) — external tenant (F3.T1.1)
- [`../../environments/secrets-and-databases.md`](../../environments/secrets-and-databases.md) — per-environment secrets
