import { InteractionRequiredAuthError, InteractionStatus } from '@azure/msal-browser'
import { renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createAccount, createUseMsalReturn } from '../test/msalTestDoubles'

vi.mock('@azure/msal-react', () => ({
  useMsal: vi.fn(),
}))

vi.mock('./returnUrl', () => ({
  clearAuthReturnUrl: vi.fn(),
}))

import { useMsal } from '@azure/msal-react'
import { clearAuthReturnUrl } from './returnUrl'
import { useVitalNexusAuth } from './useVitalNexusAuth'

describe('useVitalNexusAuth', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('starts login redirect with openid/profile scopes only', async () => {
    const msal = createUseMsalReturn()
    vi.mocked(useMsal).mockReturnValue(msal)

    const { result } = renderHook(() => useVitalNexusAuth())

    await result.current.signIn(' clinician@example.com ')

    expect(msal.instance.loginRedirect).toHaveBeenCalledWith({
      scopes: ['openid', 'profile'],
      loginHint: 'clinician@example.com',
    })
  })

  it('starts CIAM sign-up redirect with create prompt', async () => {
    vi.stubEnv('VITE_B2C_TENANT_KIND', 'ciam')

    const msal = createUseMsalReturn()
    vi.mocked(useMsal).mockReturnValue(msal)

    const { result } = renderHook(() => useVitalNexusAuth())

    await result.current.signUp('new.user@example.com')

    expect(msal.instance.loginRedirect).toHaveBeenCalledWith(
      expect.objectContaining({
        prompt: 'create',
        loginHint: 'new.user@example.com',
      }),
    )
  })

  it('clears stored return URLs before logout redirect', async () => {
    const account = createAccount()
    const msal = createUseMsalReturn({ accounts: [account] })
    vi.mocked(useMsal).mockReturnValue(msal)

    const { result } = renderHook(() => useVitalNexusAuth())

    await result.current.signOut()

    expect(clearAuthReturnUrl).toHaveBeenCalled()
    expect(msal.instance.logoutRedirect).toHaveBeenCalledWith({
      account,
      postLogoutRedirectUri: `${window.location.origin}/sign-in`,
    })
  })

  it('acquires API tokens silently when possible', async () => {
    const account = createAccount()
    const msal = createUseMsalReturn({ accounts: [account] })
    vi.mocked(msal.instance.acquireTokenSilent).mockResolvedValue({
      accessToken: 'api-token',
    } as never)

    vi.mocked(useMsal).mockReturnValue(msal)

    const { result } = renderHook(() => useVitalNexusAuth())

    await expect(result.current.acquireAccessToken()).resolves.toEqual(
      expect.objectContaining({ accessToken: 'api-token' }),
    )
  })

  it('falls back to redirect when silent token acquisition requires interaction', async () => {
    const account = createAccount()
    const msal = createUseMsalReturn({ accounts: [account] })
    vi.mocked(msal.instance.acquireTokenSilent).mockRejectedValue(
      new InteractionRequiredAuthError('interaction_required'),
    )

    vi.mocked(useMsal).mockReturnValue(msal)

    const { result } = renderHook(() => useVitalNexusAuth())

    await expect(result.current.acquireAccessToken()).rejects.toThrow(
      'Redirecting to complete API authentication.',
    )
    expect(msal.instance.acquireTokenRedirect).toHaveBeenCalledWith(
      expect.objectContaining({ account }),
    )
  })

  it('reports loading while MSAL interaction is in progress', () => {
    const msal = createUseMsalReturn({ inProgress: InteractionStatus.HandleRedirect })
    vi.mocked(useMsal).mockReturnValue(msal)

    const { result } = renderHook(() => useVitalNexusAuth())

    expect(result.current.isLoading).toBe(true)
    expect(result.current.isAuthenticated).toBe(false)
  })
})
