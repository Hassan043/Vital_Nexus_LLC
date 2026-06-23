import { afterEach, describe, expect, it, vi } from 'vitest'

afterEach(() => {
  vi.unstubAllEnvs()
})

async function loadAuthConfig() {
  vi.resetModules()
  return import('./authConfig')
}

function stubCiamEnv() {
  vi.stubEnv('VITE_B2C_CLIENT_ID', 'spa-client-id')
  vi.stubEnv('VITE_B2C_TENANT_ID', '00000000-0000-4000-8000-000000000001')
  vi.stubEnv('VITE_B2C_TENANT_DOMAIN_PREFIX', 'vitalnexusexternal')
  vi.stubEnv('VITE_B2C_TENANT_KIND', 'ciam')
  vi.stubEnv('VITE_B2C_API_SCOPE', 'https://vitalnexusexternal.onmicrosoft.com/vitalnexus-api/access_as_user')
  vi.stubEnv('VITE_B2C_REDIRECT_URI', 'http://localhost:5173/')
}

describe('authConfig', () => {
  it('detects when MSAL env vars are missing', async () => {
    vi.stubEnv('VITE_B2C_CLIENT_ID', '')
    vi.stubEnv('VITE_B2C_TENANT_ID', '')

    const { isMsalConfigured } = await loadAuthConfig()

    expect(isMsalConfigured()).toBe(false)
  })

  it('builds CIAM authority with /v2.0 suffix', async () => {
    stubCiamEnv()

    const { buildAuthority, buildKnownAuthorities } = await loadAuthConfig()

    expect(buildAuthority()).toBe(
      'https://vitalnexusexternal.ciamlogin.com/00000000-0000-4000-8000-000000000001/v2.0',
    )
    expect(buildKnownAuthorities()).toEqual(['vitalnexusexternal.ciamlogin.com'])
  })

  it('builds legacy B2C authority from user flow settings', async () => {
    vi.stubEnv('VITE_B2C_CLIENT_ID', 'spa-client-id')
    vi.stubEnv('VITE_B2C_TENANT_ID', '00000000-0000-4000-8000-000000000001')
    vi.stubEnv('VITE_B2C_TENANT_DOMAIN_PREFIX', 'vitalnexusdev')
    vi.stubEnv('VITE_B2C_TENANT_KIND', 'b2c')
    vi.stubEnv('VITE_B2C_USER_FLOW', 'B2C_1_VitalNexusSignUpSignIn')

    const { buildAuthority, buildKnownAuthorities } = await loadAuthConfig()

    expect(buildAuthority()).toBe(
      'https://vitalnexusdev.b2clogin.com/vitalnexusdev.onmicrosoft.com/B2C_1_VitalNexusSignUpSignIn',
    )
    expect(buildKnownAuthorities()).toEqual(['vitalnexusdev.b2clogin.com'])
  })

  it('keeps login scopes limited to openid and profile', async () => {
    stubCiamEnv()

    const { buildLoginRequest, loginRequest } = await loadAuthConfig()

    expect(loginRequest.scopes).toEqual(['openid', 'profile'])
    expect(buildLoginRequest()).toEqual(loginRequest)
    expect(buildLoginRequest(' clinician@example.com ')).toEqual({
      scopes: ['openid', 'profile'],
      loginHint: 'clinician@example.com',
    })
  })

  it('uses CIAM create prompt for sign-up requests', async () => {
    stubCiamEnv()

    const { buildSignUpRequest } = await loadAuthConfig()

    expect(buildSignUpRequest()).toMatchObject({
      scopes: ['openid', 'profile'],
      prompt: 'create',
    })
    expect(buildSignUpRequest('new.user@example.com')).toMatchObject({
      loginHint: 'new.user@example.com',
    })
  })

  it('uses B2C signup query parameter for sign-up requests', async () => {
    vi.stubEnv('VITE_B2C_CLIENT_ID', 'spa-client-id')
    vi.stubEnv('VITE_B2C_TENANT_ID', '00000000-0000-4000-8000-000000000001')
    vi.stubEnv('VITE_B2C_TENANT_DOMAIN_PREFIX', 'vitalnexusdev')
    vi.stubEnv('VITE_B2C_TENANT_KIND', 'b2c')
    vi.stubEnv('VITE_B2C_USER_FLOW', 'B2C_1_VitalNexusSignUpSignIn')

    const { buildSignUpRequest } = await loadAuthConfig()

    expect(buildSignUpRequest()).toMatchObject({
      extraQueryParameters: { option: 'signup' },
    })
  })

  it('adds remediation guidance for AADSTS500207 errors', async () => {
    const { formatAuthErrorMessage } = await loadAuthConfig()

    const message = formatAuthErrorMessage(new Error('AADSTS500207: account exists in another tenant'))

    expect(message).toContain('AADSTS500207')
    expect(message).toContain('/v2.0')
    expect(message).toContain('grant-b2c-spa-api-permission.ps1')
  })
})
