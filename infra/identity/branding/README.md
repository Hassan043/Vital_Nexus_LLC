# Branded authentication experience (F3.T1.5)

Configures **company branding** for VitalNexus customer sign-in and sign-up pages in each environment's Microsoft Entra External ID tenant. VitalNexus does not host authentication UI — Entra renders the pages; this task applies colors, text, and optional logos via Microsoft Graph.

## What gets configured

| Item | CIAM (dev) | Legacy B2C (test/prod) |
|------|------------|------------------------|
| Background / header colors | `organization/branding` | Same |
| Sign-in titles and descriptions | `contentCustomization.attributeCollection` | Same + optional user-flow language overrides |
| Footer / hint text | `signInPageText`, `usernameHintText` | Same |
| Logos / favicon | PUT stream properties (when asset files exist) | Same |

Per-environment values are in [`environments.json`](environments.json). Optional image paths are under [`assets/`](assets/).

## Prerequisites

1. **F3.T1.1** — external tenant exists.
2. **F3.T1.4** — sign-up/sign-in user flow configured (legacy B2C string overrides require the flow).
3. **Management application** with Microsoft Graph **application permissions** (admin consented):

| Permission | Purpose |
|------------|---------|
| `OrganizationalBranding.ReadWrite.All` | Company branding (required) |
| `Application.ReadWrite.All` | Other F3 tasks |
| `IdentityUserFlow.ReadWrite.All` | Legacy B2C user-flow language strings only |

4. GitHub Environment secrets (or local `.env`):

| Secret | Purpose |
|--------|---------|
| `B2C_TENANT_ID` | External tenant GUID |
| `B2C_MANAGEMENT_CLIENT_ID` | Management app client ID |
| `B2C_MANAGEMENT_CLIENT_SECRET` | Management app secret |

## Configure (script)

```powershell
$env:B2C_TENANT_ID = '<tenant-guid>'
$env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
$env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'

.\scripts\configure-b2c-auth-branding.ps1 -Environment dev
```

The script is **idempotent**: it PATCHes branding strings and colors, creates the English localization if missing, uploads logo files when present under `assets/`, and (legacy B2C only) applies user-flow language overrides.

Skip user-flow page overrides unless you are on a legacy B2C tenant and need both company branding and user-flow strings:

```powershell
.\scripts\configure-b2c-auth-branding.ps1 -Environment test -ApplyLegacyUserFlowStrings
```

## Verify

```powershell
.\scripts\verify-b2c-auth-branding.ps1 -Environment dev
```

Checks Graph branding properties and expected sign-in title text.

## GitHub Actions

Workflow: [`.github/workflows/configure-b2c-auth-branding.yml`](../../../.github/workflows/configure-b2c-auth-branding.yml)

Manual dispatch → select environment.

## Troubleshooting

| Graph error | Meaning | Action |
|-------------|---------|--------|
| `Authorization_RequestDenied` | Missing `OrganizationalBranding.ReadWrite.All` or consent not granted in the **external** tenant | Add permission on management app → grant admin consent |
| `Request_ResourceNotFound` on GET branding | Branding not initialized yet (normal on new tenants) | Re-run configure — POST `branding/localizations` bootstraps it |
| PATCH default branding fails | Default object not created yet | Configure script applies English localization first; optional one-time portal visit to **Company branding** |

1. **Microsoft Entra External ID** → external tenant → **Company branding**
2. Upload banner logo, set background color, customize **Text** tab strings
3. Preview sign-in page

## Related

- [`../user-flows/README.md`](../user-flows/README.md) — sign-up/sign-in user flow (F3.T1.4)
- [`../README.md`](../README.md) — external tenant (F3.T1.1)

## Next steps

- **F3.T1.6+** — MFA, password reset, SPA→API permission grant, Key Vault settings sync
- **F3.T2.x** — MSAL frontend and backend JWT validation
