import { useMemo, useRef } from 'react'
import { getApiBaseUrl } from './config'
import { createApiClient } from './apiClient'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

export function useApiClient() {
  const { acquireAccessToken } = useVitalNexusAuth()
  const acquireAccessTokenRef = useRef(acquireAccessToken)
  acquireAccessTokenRef.current = acquireAccessToken

  return useMemo(
    () =>
      createApiClient({
        baseUrl: getApiBaseUrl(),
        getAccessToken: async () => {
          const result = await acquireAccessTokenRef.current()
          return result.accessToken
        },
      }),
    [],
  )
}
