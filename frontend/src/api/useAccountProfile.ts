import { useEffect, useState } from 'react'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'
import { useApiClient } from './useApiClient'
import { getCurrentAccount, type AccountProfile } from './accountApi'
import { ApiError } from './apiClient'

type AccountProfileState = {
  profile: AccountProfile | null
  loading: boolean
  error: string
  refresh: () => void
}

export function useAccountProfile(): AccountProfileState {
  const { account } = useVitalNexusAuth()
  const api = useApiClient()
  const [profile, setProfile] = useState<AccountProfile | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [refreshKey, setRefreshKey] = useState(0)
  const accountKey = account?.homeAccountId ?? null

  useEffect(() => {
    if (!accountKey) {
      setProfile(null)
      setError('')
      return
    }

    let cancelled = false
    setLoading(true)
    setError('')

    getCurrentAccount(api)
      .then((result) => {
        if (!cancelled) {
          setProfile(result)
        }
      })
      .catch((caught: unknown) => {
        if (cancelled) {
          return
        }

        if (caught instanceof ApiError) {
          setError(`API ${caught.status}: ${caught.body || caught.message}`)
          return
        }

        setError(caught instanceof Error ? caught.message : 'API request failed.')
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [accountKey, api, refreshKey])

  return {
    profile,
    loading,
    error,
    refresh: () => setRefreshKey((value) => value + 1),
  }
}
