import { EventType, PublicClientApplication } from '@azure/msal-browser'
import { buildMsalConfiguration, formatAuthErrorMessage, isMsalConfigured } from './authConfig'

let msalInstance: PublicClientApplication | null = null

export function getMsalInstance(): PublicClientApplication {
  if (!isMsalConfigured()) {
    throw new Error('MSAL is not configured. Set VITE_B2C_CLIENT_ID and VITE_B2C_TENANT_ID.')
  }

  if (!msalInstance) {
    msalInstance = new PublicClientApplication(buildMsalConfiguration())
    msalInstance.addEventCallback((event) => {
      if (
        event.eventType === EventType.LOGIN_SUCCESS &&
        event.payload &&
        'account' in event.payload &&
        event.payload.account
      ) {
        msalInstance?.setActiveAccount(event.payload.account)
      }
    })
  }

  return msalInstance
}

export async function initializeMsal(): Promise<PublicClientApplication> {
  const instance = getMsalInstance()
  await instance.initialize()

  try {
    const redirectResult = await instance.handleRedirectPromise()
    if (redirectResult?.account) {
      instance.setActiveAccount(redirectResult.account)
    } else {
      const accounts = instance.getAllAccounts()
      if (!instance.getActiveAccount() && accounts.length > 0) {
        instance.setActiveAccount(accounts[0])
      }
    }
  } catch (error) {
    throw new Error(formatAuthErrorMessage(error))
  }

  return instance
}
