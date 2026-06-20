import type { ApiClient } from './apiClient'

export type AccountProfile = {
  objectId: string | null
  name: string | null
  email: string | null
  scopes: string | null
}

export async function getCurrentAccount(api: ApiClient): Promise<AccountProfile> {
  return api.get<AccountProfile>('/api/me')
}
