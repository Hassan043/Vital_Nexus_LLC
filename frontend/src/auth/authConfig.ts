import type { Configuration } from '@azure/msal-browser'

export type TenantKind = 'ciam' | 'b2c'

function readEnv(name: keyof ImportMetaEnv): string {
  return (import.meta.env[name] ?? '').trim()
}

export function getTenantKind(): TenantKind {
  const kind = readEnv('VITE_B2C_TENANT_KIND').toLowerCase()
  return kind === 'b2c' ? 'b2c' : 'ciam'
}

export function isMsalConfigured(): boolean {
  return Boolean(readEnv('VITE_B2C_CLIENT_ID') && readEnv('VITE_B2C_TENANT_ID'))
}

export function buildAuthority(): string {
  const tenantId = readEnv('VITE_B2C_TENANT_ID')
  const domainPrefix = readEnv('VITE_B2C_TENANT_DOMAIN_PREFIX')

  if (!tenantId) {
    throw new Error('VITE_B2C_TENANT_ID is required.')
  }

  if (getTenantKind() === 'b2c') {
    const userFlow = readEnv('VITE_B2C_USER_FLOW')
    if (!domainPrefix || !userFlow) {
      throw new Error(
        'VITE_B2C_TENANT_DOMAIN_PREFIX and VITE_B2C_USER_FLOW are required for legacy B2C.',
      )
    }

    return `https://${domainPrefix}.b2clogin.com/${domainPrefix}.onmicrosoft.com/${userFlow}`
  }

  if (!domainPrefix) {
    throw new Error('VITE_B2C_TENANT_DOMAIN_PREFIX is required for CIAM.')
  }

  return `https://${domainPrefix}.ciamlogin.com/${tenantId}/v2.0`
}

export function buildKnownAuthorities(): string[] {
  const domainPrefix = readEnv('VITE_B2C_TENANT_DOMAIN_PREFIX')
  if (!domainPrefix) {
    return []
  }

  return getTenantKind() === 'b2c'
    ? [`${domainPrefix}.b2clogin.com`]
    : [`${domainPrefix}.ciamlogin.com`]
}

export function getRedirectUri(): string {
  const configured = readEnv('VITE_B2C_REDIRECT_URI')
  if (configured) {
    return configured
  }

  if (typeof window !== 'undefined') {
    return window.location.origin
  }

  return 'http://localhost:5173/'
}

export function getApiScope(): string | undefined {
  const scope = readEnv('VITE_B2C_API_SCOPE')
  return scope || undefined
}

export function buildMsalConfiguration(): Configuration {
  const clientId = readEnv('VITE_B2C_CLIENT_ID')
  if (!clientId) {
    throw new Error('VITE_B2C_CLIENT_ID is required.')
  }

  const redirectUri = getRedirectUri()

  return {
    auth: {
      clientId,
      authority: buildAuthority(),
      knownAuthorities: buildKnownAuthorities(),
      redirectUri,
      postLogoutRedirectUri: redirectUri,
    },
    cache: {
      cacheLocation: 'sessionStorage',
    },
  }
}

export const loginRequest = {
  scopes: ['openid', 'profile'],
}

export function buildLoginRequest(email?: string) {
  const trimmedEmail = email?.trim()
  if (!trimmedEmail) {
    return loginRequest
  }

  return {
    ...loginRequest,
    loginHint: trimmedEmail,
  }
}

export const signUpRequest = {
  scopes: ['openid', 'profile'],
  ...(getTenantKind() === 'ciam'
    ? { prompt: 'create' as const }
    : { extraQueryParameters: { option: 'signup' } }),
}

export function buildSignUpRequest(email?: string) {
  const trimmedEmail = email?.trim()
  if (!trimmedEmail) {
    return signUpRequest
  }

  return {
    ...signUpRequest,
    loginHint: trimmedEmail,
  }
}

export const tokenRequest = {
  scopes: [getApiScope()].filter((scope): scope is string => Boolean(scope)),
}

export function formatAuthErrorMessage(error: unknown): string {
  const message = error instanceof Error ? error.message : String(error)

  if (message.includes('AADSTS500207')) {
    return `${message}

CIAM sign-in fixes:
  1. Authority must end with /v2.0 (updated in authConfig).
  2. API scope is requested only after sign-in, not on login redirect.
  3. API app must be single-tenant; re-run .\\scripts\\register-b2c-api-app.ps1 -Environment dev
  4. Grant SPA delegated API permission: .\\scripts\\grant-b2c-spa-api-permission.ps1 -Environment dev`
  }

  return message
}
