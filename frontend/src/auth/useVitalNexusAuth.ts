import { InteractionRequiredAuthError, InteractionStatus } from '@azure/msal-browser'
import { useCallback } from 'react'
import { useMsal } from '@azure/msal-react'
import { buildLoginRequest, buildSignUpRequest, tokenRequest } from './authConfig'

export function useVitalNexusAuth() {
  const { instance, accounts, inProgress } = useMsal()
  const account = instance.getActiveAccount() ?? accounts[0] ?? null
  const isAuthenticated = account !== null
  const isLoading = inProgress !== InteractionStatus.None

  async function signIn(email?: string): Promise<void> {
    await instance.loginRedirect(buildLoginRequest(email))
  }

  async function signUp(email?: string): Promise<void> {
    await instance.loginRedirect(buildSignUpRequest(email))
  }

  async function signOut(): Promise<void> {
    await instance.logoutRedirect({
      account: account ?? undefined,
    })
  }

  const acquireAccessToken = useCallback(async () => {
    if (!account) {
      throw new Error('No signed-in account is available.')
    }

    try {
      return await instance.acquireTokenSilent({
        ...tokenRequest,
        account,
      })
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        await instance.acquireTokenRedirect({
          ...tokenRequest,
          account,
        })
        throw new Error('Redirecting to complete API authentication.')
      }

      throw error
    }
  }, [account, instance])

  return {
    account,
    isAuthenticated,
    isLoading,
    signIn,
    signUp,
    signOut,
    acquireAccessToken,
  }
}
