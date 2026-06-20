import { useMemo } from 'react'
import { getApiBaseUrl } from './config'
import { createApiClient } from './apiClient'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

export function useApiClient() {
  const { acquireAccessToken } = useVitalNexusAuth()

  return useMemo(
    () =>
      createApiClient({
        baseUrl: getApiBaseUrl(),
        getAccessToken: async () => {
          const result = await acquireAccessToken()
          return result.accessToken
        },
      }),
    [acquireAccessToken],
  )
}
