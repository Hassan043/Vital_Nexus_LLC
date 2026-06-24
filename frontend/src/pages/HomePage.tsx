import { useCallback, useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { getWorkspaceDashboard, isOnboardingComplete, type OnboardingDashboard } from '../api/accountApi'
import { useAccountProfile } from '../api/useAccountProfile'
import { useApiClient } from '../api/useApiClient'
import { ApiError } from '../api/apiClient'
import { isAdmin } from '../auth/roles'
import { WorkspaceDashboard } from '../components/WorkspaceDashboard'

export function HomePage() {
  const api = useApiClient()
  const { profile, loading: profileLoading, error: profileError, refresh: refreshProfile } = useAccountProfile()
  const [dashboard, setDashboard] = useState<OnboardingDashboard | null>(null)
  const [dashboardLoading, setDashboardLoading] = useState(true)
  const [dashboardError, setDashboardError] = useState('')
  const userIsAdmin = isAdmin(profile?.roles)
  const profileKey = profile?.userId ?? profile?.email ?? null

  const loadDashboard = useCallback(async () => {
    if (!userIsAdmin) {
      setDashboard(null)
      setDashboardError('')
      setDashboardLoading(false)
      return
    }

    setDashboardLoading(true)
    setDashboardError('')
    try {
      const result = await getWorkspaceDashboard(api)
      setDashboard(result)
    } catch (caught: unknown) {
      setDashboard(null)
      if (caught instanceof ApiError) {
        setDashboardError(caught.displayMessage)
      } else {
        setDashboardError(caught instanceof Error ? caught.message : 'Failed to load organization data.')
      }
    } finally {
      setDashboardLoading(false)
    }
  }, [api, userIsAdmin])

  useEffect(() => {
    if (profileLoading) {
      return
    }

    if (!profileKey) {
      setDashboardLoading(false)
      return
    }

    void loadDashboard()
  }, [profileLoading, profileKey, loadDashboard])

  if (profileLoading || (userIsAdmin && dashboardLoading)) {
    return (
      <div className="table-page">
        <p className="table-message">Loading your workspace…</p>
      </div>
    )
  }

  if (userIsAdmin && dashboard && !isOnboardingComplete(dashboard.onboarding)) {
    return <Navigate to="/onboarding" replace />
  }

  function handleRefresh() {
    refreshProfile()
    void loadDashboard()
  }

  return (
    <div className="table-page">
      {dashboardError || profileError ? (
        <div className="table-message table-message-error workspace-error">
          <p>{dashboardError || profileError}</p>
          <button type="button" className="table-button" onClick={handleRefresh}>
            Retry
          </button>
        </div>
      ) : null}
      <WorkspaceDashboard
        profile={profile}
        dashboard={dashboard}
        dashboardLoading={false}
        dashboardError=""
        onRefresh={handleRefresh}
      />
    </div>
  )
}
