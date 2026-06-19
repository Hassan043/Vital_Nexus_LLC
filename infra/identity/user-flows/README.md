# Customer sign-up and sign-in user flow (F3.T1.4)



Configures the VitalNexus **combined sign-up/sign-in** user flow in each environment's Microsoft Entra External ID tenant. Credential handling, MFA enrollment UI, and password validation remain in Entra — VitalNexus does not implement register/login APIs.



## Tenant kinds



| Kind | API | Login host | Flow identifier |

|------|-----|------------|-----------------|

| **ciam** | `identity/authenticationEventsFlows` | `{prefix}.ciamlogin.com/{tenant-id}` | GUID (display name in config) |

| **b2c** | `identity/b2cUserFlows` | `{prefix}.b2clogin.com/{domain}/{flow-id}` | `B2C_1_*` |



Dev uses **CIAM** (`tenantKind: "ciam"` in [`environments.json`](environments.json)). Test/prod default to legacy B2C until those tenants exist.



## User flow



| Setting | CIAM (dev) | Legacy B2C (test/prod) |

|---------|------------|------------------------|

| Display name | `VitalNexus Dev Sign Up Sign In` | — |

| Short ID | — | `VitalNexusSignUpSignIn` |

| Full ID | GUID after create | `B2C_1_VitalNexusSignUpSignIn` |

| Type | `externalUsersSelfServiceSignUpEventsFlow` | `signUpOrSignIn` (v3) |

| Identity provider | Email + password | Email + password (local account) |



Per-environment settings are in [`environments.json`](environments.json).



## Prerequisites



1. **F3.T1.1** — external tenant exists for the target environment.

2. **F3.T1.2** / **F3.T1.3** — SPA and API app registrations (MSAL wiring is later).

3. **Management application** in the external tenant with Microsoft Graph **application permissions** (admin consented):



| Permission | CIAM | Legacy B2C |

|------------|------|------------|

| `Application.ReadWrite.All` | Yes | Yes |

| `EventListener.ReadWrite.All` or `Policy.ReadWrite.AuthenticationFlows` | Yes | — |

| `IdentityUserFlow.ReadWrite.All` | — | Yes |



4. GitHub Environment secrets (or local `.env`):



| Secret | Purpose |

|--------|---------|

| `B2C_TENANT_ID` | External tenant GUID |

| `B2C_MANAGEMENT_CLIENT_ID` | Management app client ID |

| `B2C_MANAGEMENT_CLIENT_SECRET` | Management app secret |

| `B2C_SPA_CLIENT_ID` | Optional; links SPA app to CIAM flow on create |



## Configure (script)



```powershell

$env:B2C_TENANT_ID = '<tenant-guid>'

$env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'

$env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'

$env:B2C_SPA_CLIENT_ID = '<spa-client-id>'   # optional for CIAM



.\scripts\configure-b2c-signup-signin-flow.ps1 -Environment dev

```



The script is **idempotent**: it creates the user flow if missing. Legacy B2C tenants also enable English language customization.



## Verify



```powershell

.\scripts\verify-b2c-user-flow.ps1 -Environment dev

```



- **CIAM:** checks Graph for the flow by display name, then fetches OpenID metadata from `{prefix}.ciamlogin.com/{tenant-id}/v2.0/.well-known/openid-configuration`

- **Legacy B2C:** issuer contains `B2C_1_VitalNexusSignUpSignIn`



## Branding



| Item | How |

|------|-----|

| Page titles / welcome text | F3.T1.5 company branding — [`../branding/README.md`](../branding/README.md) |

| Logo, banner, colors | F3.T1.5 script or Azure portal → **Company branding** |

| Layout | Azure portal → User flows → **Page layouts** (optional) |



Binary assets (logo PNG) are uploaded in the portal. Do not commit customer-facing image binaries to git unless the team standardizes on a shared assets pipeline.



## GitHub Actions



Workflow: [`.github/workflows/configure-b2c-user-flow.yml`](../../../.github/workflows/configure-b2c-user-flow.yml)



Manual dispatch → select environment. Requires B2C management secrets in that GitHub Environment.



## Portal steps (manual alternative)



### CIAM (Entra External ID)



1. **Microsoft Entra External ID** → external tenant → **External Identities** → **User flows**

2. **New user flow** → **Sign up and sign in**

3. Name: `VitalNexus Dev Sign Up Sign In` → identity provider **Email with password**

4. **Create** → optionally assign the VitalNexus Frontend app

5. Optional: **Company branding** and page layout customization



### Legacy B2C



1. **Microsoft Entra External ID** → external tenant → **External Identities** → **User flows**

2. **New user flow** → **Sign up and sign in** → version **Recommended**

3. Name: `VitalNexusSignUpSignIn` → identity provider **Email signup**

4. **Create** → enable language customization → add English

5. Optional: **Company branding** and page layout customization



## Related



- [`../README.md`](../README.md) — external tenant (F3.T1.1)

- [`../spa-app/README.md`](../spa-app/README.md) — SPA app (F3.T1.2)

- [`../api-app/README.md`](../api-app/README.md) — API scopes (F3.T1.3)



## Next steps



- **F3.T1.5** — Branded authentication experience — [`../branding/README.md`](../branding/README.md)

- **F3.T1.6** — Password recovery — [`../password-recovery/README.md`](../password-recovery/README.md)

- **F3.T1.7+** — MFA, environment settings / Key Vault sync

- **F3.T2.x** — MSAL frontend and backend JWT validation targeting this user flow


