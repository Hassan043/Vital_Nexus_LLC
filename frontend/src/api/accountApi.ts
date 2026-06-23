import type { ApiClient } from './apiClient'

export type AccountProfile = {
  userId?: string | null
  customerId?: string | null
  objectId: string | null
  name: string | null
  email: string | null
  scopes: string | null
  roles?: string[]
}

export type AdminAccountOverview = {
  customerId: string
  customerName: string
  userId: string
  email: string
  displayName: string | null
  roles: string[]
  access: string
}

export async function getCurrentAccount(api: ApiClient): Promise<AccountProfile> {
  return api.get<AccountProfile>('/api/me')
}

export async function getAdminAccountOverview(api: ApiClient): Promise<AdminAccountOverview> {
  return api.get<AdminAccountOverview>('/api/admin/account')
}
