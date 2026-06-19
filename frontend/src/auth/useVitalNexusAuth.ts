import { InteractionStatus } from '@azure/msal-browser'
import { useMsal } from '@azure/msal-react'
import { loginRequest, tokenRequest } from './authConfig'

export function useVitalNexusAuth() {
  const { instance, accounts, inProgress } = useMsal()
  const account = instance.getActiveAccount() ?? accounts[0] ?? null
  const isAuthenticated = account !== null
  const isLoading = inProgress !== InteractionStatus.None

  async function signIn(): Promise<void> {
    await instance.loginRedirect(loginRequest)
  }

  async function signOut(): Promise<void> {
    await instance.logoutRedirect({
      account: account ?? undefined,
    })
  }

  async function acquireAccessToken() {
    if (!account) {
      throw new Error('No signed-in account is available.')
    }

    return instance.acquireTokenSilent({
      ...tokenRequest,
      account,
    })
  }

  return {
    account,
    isAuthenticated,
    isLoading,
    signIn,
    signOut,
    acquireAccessToken,
  }
}
