# External ID MFA via Conditional Access (F3.T1.7)

Configures **multifactor authentication (MFA)** for customer sign-in using Microsoft Entra **Conditional Access** in each external tenant. VitalNexus does **not** implement TOTP, SMS gateways, or MFA enrollment UI — Entra handles second-factor verification.

## Tenant kinds

| Kind | MFA model |
|------|-----------|
| **ciam** (dev) | Conditional Access policy requiring MFA + Email OTP as second factor |
| **b2c** (test/prod config) | Same CA policy via Graph where supported; user-flow MFA enrollment may require portal if CA is unavailable |

Requires **F3.T1.4** sign-up/sign-in user flow with **Email + password** (Email OTP cannot be MFA second factor when Email OTP is the primary sign-in method).

## What the configure script does

1. Ensures **Email OTP** authentication method is enabled for all users
2. Creates or updates a **Conditional Access** policy that:
   - Targets **all users** (configurable)
   - Targets the **VitalNexus SPA** application (`B2C_SPA_CLIENT_ID`)
   - **Requires multifactor authentication** on grant

## Prerequisites

1. **F3.T1.2** — SPA app registered; `B2C_SPA_CLIENT_ID` in secrets
2. **F3.T1.4** — sign-up/sign-in user flow with Email + password
3. Management app Graph **application permissions** (admin consented in **vitalnexusexternal**):

| Permission | Required | Purpose |
|------------|----------|---------|
| `Policy.Read.All` | **Yes** | List/read Conditional Access policies |
| `Policy.ReadWrite.ConditionalAccess` | **Yes** | Create/update MFA Conditional Access policy |
| `Application.Read.All` | **Yes** | Conditional Access app targeting (Microsoft requires all three for app-only CA) |
| `Policy.ReadWrite.AuthenticationMethod` | Yes | Enable Email OTP MFA method |

4. **Security Defaults must be disabled** in the external tenant before any Conditional Access policy can be created. The configure script disables it automatically via Graph (`Policy.ReadWrite.ConditionalAccess` is sufficient). Security Defaults and custom CA policies are mutually exclusive.

5. Secrets:

| Secret | Purpose |
|--------|---------|
| `B2C_TENANT_ID` | External tenant GUID |
| `B2C_MANAGEMENT_CLIENT_ID` | Management app client ID |
| `B2C_MANAGEMENT_CLIENT_SECRET` | Management app secret |
| `B2C_SPA_CLIENT_ID` | SPA app to target in Conditional Access |

## Configure

```powershell
$env:B2C_TENANT_ID = '<tenant-guid>'
$env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
$env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'
$env:B2C_SPA_CLIENT_ID = '<spa-client-id>'

.\scripts\configure-b2c-mfa-conditional-access.ps1 -Environment dev
```

Report-only mode (policy created as disabled for testing):

```powershell
.\scripts\configure-b2c-mfa-conditional-access.ps1 -Environment dev -ReportOnly
```

## Verify

```powershell
.\scripts\verify-b2c-mfa-conditional-access.ps1 -Environment dev
```

Manual test: sign in → complete password → prompted for Email OTP second factor.

## GitHub Actions

[`.github/workflows/configure-b2c-mfa-conditional-access.yml`](../../../.github/workflows/configure-b2c-mfa-conditional-access.yml)

## Portal fallback

1. **Properties** → **Manage security defaults** → **Disabled** (required before CA)
2. **Authentication methods** → **Email OTP** → Enable → All users
3. **Conditional Access** → New policy → Users: All → Apps: VitalNexus Frontend → Grant: Require MFA
4. Legacy B2C only: **User flows** → your sign-up/sign-in flow → MFA → **Conditional** enforcement + Email OTP

## Related

- [`../user-flows/README.md`](../user-flows/README.md) — F3.T1.4
- [`../password-recovery/README.md`](../password-recovery/README.md) — F3.T1.6 (Email OTP also used for SSPR)

## Next steps

- **F3.T1.8+** — SPA→API permission grant, Key Vault settings sync
- **F3.T2.x** — MSAL frontend and backend JWT validation
