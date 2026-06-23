import { useAccountProfile } from '../api/useAccountProfile'
import { getApiBaseUrl } from '../api/config'
import { isAdmin } from '../auth/roles'
import { AdminAccountPanel } from '../components/AdminAccountPanel'

export function HomePage() {
  const { profile, loading: apiLoading, error: apiError } = useAccountProfile()
  const roleLabel = profile?.roles?.join(', ') ?? 'none'
  const userIsAdmin = isAdmin(profile?.roles)

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
    </>
  )
}
