import { useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { getOnboardingDashboard } from '../api/accountApi'
import { useAccountProfile } from '../api/useAccountProfile'
import { useApiClient } from '../api/useApiClient'
import { getApiBaseUrl } from '../api/config'
import { isAdmin } from '../auth/roles'
import { AdminAccountPanel } from '../components/AdminAccountPanel'
import { CustomerOnboardingDemo } from '../components/CustomerOnboardingDemo'

export function HomePage() {
  const api = useApiClient()
  const { profile, loading: apiLoading, error: apiError } = useAccountProfile()
  const [onboardingComplete, setOnboardingComplete] = useState<boolean | null>(null)
  const roleLabel = profile?.roles?.join(', ') ?? 'none'
  const userIsAdmin = isAdmin(profile?.roles)

  useEffect(() => {
    if (!userIsAdmin) {
      setOnboardingComplete(true)
      return
    }

    let cancelled = false
    getOnboardingDashboard(api)
      .then((dashboard) => {
        if (!cancelled) {
          setOnboardingComplete(dashboard.onboarding.isComplete)
        }
      })
      .catch(() => {
        if (!cancelled) {
          setOnboardingComplete(true)
        }
      })

    return () => {
      cancelled = true
    }
  }, [api, userIsAdmin])

  if (userIsAdmin && onboardingComplete === false) {
    return <Navigate to="/onboarding" replace />
  }

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
              {profile.name ? <p className="welcome-name">{profile.name}</p> : null}
              {profile.email ? <p className="welcome-email">{profile.email}</p> : null}
              <p className="welcome-email">Application role: {roleLabel}</p>
              <p className="welcome-email">API scopes: {profile.scopes ?? 'none'}</p>
              {!userIsAdmin ? (
                <p className="flow-note">Customer administration actions are hidden for User accounts.</p>
              ) : null}
            </>
          ) : null}
          {apiError ? <p className="field-error">{apiError}</p> : null}
        </div>
      </section>

      <AdminAccountPanel roles={profile?.roles} />
      <CustomerOnboardingDemo roles={profile?.roles} />
    </>
  )
}
