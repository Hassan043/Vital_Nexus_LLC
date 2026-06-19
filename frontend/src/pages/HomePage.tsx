import { useEffect, useState } from 'react'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'
import { useApiClient } from '../api/useApiClient'
import { getCurrentAccount, type AccountProfile } from '../api/accountApi'
import { ApiError } from '../api/apiClient'
import { getApiBaseUrl } from '../api/config'

export function HomePage() {
  const { account } = useVitalNexusAuth()
  const api = useApiClient()
  const [profile, setProfile] = useState<AccountProfile | null>(null)
  const [apiError, setApiError] = useState('')
  const [apiLoading, setApiLoading] = useState(false)

  useEffect(() => {
    if (!account) {
      setProfile(null)
      setApiError('')
      return
    }

    let cancelled = false
    setApiLoading(true)
    setApiError('')

    getCurrentAccount(api)
      .then((result) => {
        if (!cancelled) {
          setProfile(result)
        }
      })
      .catch((error: unknown) => {
        if (cancelled) {
          return
        }

        if (error instanceof ApiError) {
          setApiError(`API ${error.status}: ${error.body || error.message}`)
          return
        }

        setApiError(error instanceof Error ? error.message : 'API request failed.')
      })
      .finally(() => {
        if (!cancelled) {
          setApiLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [account, api])

  return (
    <>
      <div className="flow-header">
        <p className="eyebrow">Clinic workspace</p>
        <h1>Functional medicine lab intelligence</h1>
        <p className="lede">Your VitalNexus session is active. Protected routes and API calls use your Entra access token.</p>
      </div>

      <section className="auth-panel" aria-live="polite">
        <div className="api-status">
          <p className="welcome-label">Session check</p>
          {apiLoading ? (
            <p className="auth-status">Calling {getApiBaseUrl()}/api/me…</p>
          ) : profile ? (
            <>
              <p className="auth-status">API confirmed your access token.</p>
              <p className="welcome-email">API scopes: {profile.scopes ?? 'none'}</p>
            </>
          ) : null}
          {apiError ? <p className="field-error">{apiError}</p> : null}
        </div>
      </section>
    </>
  )
}
