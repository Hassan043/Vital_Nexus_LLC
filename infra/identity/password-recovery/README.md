# External ID password recovery (F3.T1.6)

Configures **self-service password reset (SSPR)** for customer accounts in each Microsoft Entra External ID tenant. VitalNexus does **not** implement password-reset APIs, token storage, or email delivery — Entra handles the full recovery flow.

## Tenant kinds

| Kind | Password recovery model |
|------|-------------------------|
| **ciam** (dev) | SSPR built into the sign-up/sign-in experience — Email OTP verification, **Forgot password?** link on sign-in |
| **b2c** (test/prod config) | Dedicated `passwordReset` user flow (`B2C_1_*`) plus Email OTP policy |

Requires **F3.T1.4** sign-up/sign-in user flow with **Email + password** identity provider.

## What the configure script does

### CIAM

1. Enables **Email OTP** authentication method for all users (`Policy.ReadWrite.AuthenticationMethod`)
2. Shows the **Forgot password?** link via company branding (`OrganizationalBranding.ReadWrite.All`)
3. Optionally sets custom forgot-password link text

### Legacy B2C

1. Same Email OTP + branding steps where supported
2. Creates **`passwordReset`** user flow if missing (`IdentityUserFlow.ReadWrite.All`)

## Prerequisites

Management app Graph **application permissions** (admin consented):

| Permission | Purpose |
|------------|---------|
| `Policy.ReadWrite.AuthenticationMethod` | Enable Email OTP for SSPR |
| `OrganizationalBranding.ReadWrite.All` | Show/customize forgot-password link |
| `IdentityUserFlow.ReadWrite.All` | Legacy B2C password-reset user flow only |
| `EventListener.ReadWrite.All` | F3.T1.4 CIAM user flow (existing) |

Secrets: `B2C_TENANT_ID`, `B2C_MANAGEMENT_CLIENT_ID`, `B2C_MANAGEMENT_CLIENT_SECRET`

## Configure

```powershell
$env:B2C_TENANT_ID = '<tenant-guid>'
$env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
$env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'

.\scripts\configure-b2c-password-recovery.ps1 -Environment dev
```

## Verify

```powershell
.\scripts\verify-b2c-password-recovery.ps1 -Environment dev
```

Manual test: open the app sign-in page → enter email → **Forgot password?** → complete Email OTP → set new password.

## Important constraints

- There is **no standalone password-reset URL** for onboarding emails on CIAM — users must use **Forgot password?** during sign-in (stateful flow).
- For admin-created users, use Graph `passwordProfile.forceChangePasswordNextSignIn` or direct users to SSPR.

## GitHub Actions

[`.github/workflows/configure-b2c-password-recovery.yml`](../../../.github/workflows/configure-b2c-password-recovery.yml)

## Portal fallback

1. **Authentication methods** → **Email OTP** → Enable → All users
2. **Company branding** → Sign-in form → Show self-service password reset
3. Legacy B2C: **User flows** → New → Password reset

## Related

- [`../user-flows/README.md`](../user-flows/README.md) — F3.T1.4 sign-up/sign-in
- [`../branding/README.md`](../branding/README.md) — F3.T1.5 company branding

## Next steps

- **F3.T1.7** — MFA via Conditional Access — [`../mfa/README.md`](../mfa/README.md)
- **F3.T1.8+** — SPA→API permission grant, Key Vault settings sync
- **F3.T2.x** — MSAL; legacy B2C may pass `passwordReset` user flow id to MSAL
