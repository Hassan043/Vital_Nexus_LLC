import { useEffect, useState } from 'react'
import { getAdminAccountOverview, type AdminAccountOverview } from '../api/accountApi'
import { useApiClient } from '../api/useApiClient'
import { ApiError } from '../api/apiClient'
import { isAdmin } from '../auth/roles'

type AdminAccountPanelProps = {
  roles: string[] | null | undefined
}

export function AdminAccountPanel({ roles }: AdminAccountPanelProps) {
  const api = useApiClient()
  const [overview, setOverview] = useState<AdminAccountOverview | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const canAccess = isAdmin(roles)

  useEffect(() => {
    if (!canAccess) {
      setOverview(null)
      setError('')
      return
    }

    let cancelled = false
    setLoading(true)
    setError('')

    getAdminAccountOverview(api)
      .then((result) => {
        if (!cancelled) {
          setOverview(result)
        }
      })
      .catch((caught: unknown) => {
        if (cancelled) {
          return
        }

        if (caught instanceof ApiError && caught.status === 403) {
          setError('You do not have permission to view customer administration.')
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
  }, [api, canAccess])

  if (!canAccess) {
    return null
  }

  return (
    <section className="auth-panel admin-panel" aria-live="polite">
      <p className="welcome-label">Customer administration</p>
      {loading ? <p className="auth-status">Loading admin account overview…</p> : null}
      {overview ? (
        <>
          <p className="auth-status">{overview.access}</p>
          <p className="welcome-name">{overview.customerName}</p>
          <p className="welcome-email">Customer ID: {overview.customerId}</p>
        </>
      ) : null}
      {error ? <p className="field-error">{error}</p> : null}
    </section>
  )
}
