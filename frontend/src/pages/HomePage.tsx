import { useAccountProfile } from '../api/useAccountProfile'
import { getApiBaseUrl } from '../api/config'
import { isAdmin } from '../auth/roles'
import { AdminAccountPanel } from '../components/AdminAccountPanel'

export function HomePage() {
  const { account } = useVitalNexusAuth()
  const api = useApiClient()
  const [profile, setProfile] = useState<AccountProfile | null>(null)
  const [apiError, setApiError] = useState('')
  const [apiLoading, setApiLoading] = useState(false)
  const accountKey = account?.homeAccountId ?? null

  useEffect(() => {
    if (!accountKey) {
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
  }, [accountKey, api])

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
