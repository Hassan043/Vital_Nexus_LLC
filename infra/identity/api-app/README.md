# Entra External ID API app registration — backend API scopes (F3.T1.3)

Registers the VitalNexus **backend API** in each environment's Microsoft Entra External ID tenant and exposes delegated **OAuth2 scopes** for the React SPA to request access tokens.

The API app is **not** a login client — it defines the resource identifier and scopes that appear in JWT `aud` / `scp` claims. Token validation is implemented in F3.T2.x.

## App registration settings

| Setting | Value |
|---------|-------|
| Platform | Web/API (expose an API) |
| Application ID URI | `https://<tenantDomainPrefix>.onmicrosoft.com/vitalnexus-api` |
| Scope | `access_as_user` — Access VitalNexus API as the signed-in user |
| Full scope URI | `https://<tenantDomainPrefix>.onmicrosoft.com/vitalnexus-api/access_as_user` |
| Client secret | None on the API app itself |

`tenantDomainPrefix` must match `infra/identity/main.<env>.bicepparam` for the same environment.

## Per-environment apps

Configured in [`environments.json`](environments.json):

| Environment | App name | Application ID URI |
|-------------|----------|-------------------|
| dev | VitalNexus API Dev | `https://vitalnexusdev.onmicrosoft.com/vitalnexus-api` |
| test | VitalNexus API Test | `https://vitalnexustest.onmicrosoft.com/vitalnexus-api` |
| prod | VitalNexus API | `https://vitalnexusprod.onmicrosoft.com/vitalnexus-api` |

## Prerequisites

1. **F3.T1.1** external tenant deployed.
2. **F3.T1.2** SPA app registered (SPA permission grant to this API is **F3.T1.4**).
3. Management app credentials in GitHub Environment secrets (`B2C_TENANT_ID`, `B2C_MANAGEMENT_CLIENT_ID`, `B2C_MANAGEMENT_CLIENT_SECRET`).

## Register (automated)

```powershell
$env:B2C_TENANT_ID = '<tenant-guid>'
$env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
$env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'

.\scripts\register-b2c-api-app.ps1 -Environment dev

# Optional: persist API metadata to Key Vault
.\scripts\register-b2c-api-app.ps1 -Environment dev -KeyVaultName kv-vnx-dev-<suffix>
```

## Register (GitHub Actions)

Workflow: [`.github/workflows/configure-b2c-api-app.yml`](../../../.github/workflows/configure-b2c-api-app.yml)

## Verify

```powershell
.\scripts\verify-b2c-api-app.ps1 -Environment dev
```

Confirm in the portal: **App registrations** → API app → **Expose an API** → scope `access_as_user` is listed.

## Outputs to store

| Name | Example | Where |
|------|---------|-------|
| API Client ID | `<guid>` | GitHub `B2C_API_CLIENT_ID`, Key Vault `b2c-api-client-id` |
| Application ID URI | `https://vitalnexusdev.onmicrosoft.com/vitalnexus-api` | Key Vault `b2c-api-application-id-uri` |
| Scope name | `access_as_user` | Key Vault `b2c-api-scope` |
| Full scope URI | `.../vitalnexus-api/access_as_user` | Key Vault `b2c-api-scope-uri` (MSAL `VITE_B2C_API_SCOPE`) |

## Portal steps (manual alternative)

1. **App registrations** → **New registration** → name per `environments.json`
2. **Expose an API** → set Application ID URI → **Add a scope**
3. Scope name: `access_as_user`, enable admin and user consent text from `environments.json`
4. Copy **Application (client) ID** → `B2C_API_CLIENT_ID`

Do **not** grant SPA delegated permissions here — that is **F3.T1.4**.

## Related

- [`../spa-app/README.md`](../spa-app/README.md) — React SPA (F3.T1.2)
- [`../README.md`](../README.md) — external tenant (F3.T1.1)
