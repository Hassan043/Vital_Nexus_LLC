import { InteractionStatus, Logger, type AccountInfo, type IPublicClientApplication } from '@azure/msal-browser'
import type { IMsalContext } from '@azure/msal-react'
import { vi } from 'vitest'

export function createAccount(overrides: Partial<AccountInfo> = {}): AccountInfo {
  return {
    homeAccountId: 'home-account-id',
    environment: 'login.microsoftonline.com',
    tenantId: '00000000-0000-4000-8000-000000000001',
    username: 'clinician@example.com',
    localAccountId: 'local-account-id',
    name: 'Test Clinician',
    ...overrides,
  }
}

export function createMsalInstanceMock(
  overrides: Partial<IPublicClientApplication> = {},
): IPublicClientApplication {
  return {
    getActiveAccount: vi.fn().mockReturnValue(null),
    getAllAccounts: vi.fn().mockReturnValue([]),
    setActiveAccount: vi.fn(),
    loginRedirect: vi.fn().mockResolvedValue(undefined),
    logoutRedirect: vi.fn().mockResolvedValue(undefined),
    acquireTokenSilent: vi.fn(),
    acquireTokenRedirect: vi.fn().mockResolvedValue(undefined),
    initialize: vi.fn().mockResolvedValue(undefined),
    handleRedirectPromise: vi.fn().mockResolvedValue(null),
    addEventCallback: vi.fn(),
    ...overrides,
  } as unknown as IPublicClientApplication
}

export function createUseMsalReturn(overrides: {
  instance?: Partial<IPublicClientApplication>
  accounts?: AccountInfo[]
  inProgress?: InteractionStatus
} = {}): IMsalContext {
  const instance = createMsalInstanceMock(overrides.instance)
  const accounts = overrides.accounts ?? []

  if (accounts.length > 0) {
    vi.mocked(instance.getActiveAccount).mockReturnValue(accounts[0])
    vi.mocked(instance.getAllAccounts).mockReturnValue(accounts)
  }

  return {
    instance,
    accounts,
    inProgress: overrides.inProgress ?? InteractionStatus.None,
    logger: new Logger({}, instance as never),
  }
}
