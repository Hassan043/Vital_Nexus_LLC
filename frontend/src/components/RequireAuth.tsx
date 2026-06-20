import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { saveAuthReturnUrl } from '../auth/returnUrl'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'
import { AuthLoadingScreen } from './AuthLoadingScreen'

export function RequireAuth() {
  const { isAuthenticated, isLoading } = useVitalNexusAuth()
  const location = useLocation()

  if (isLoading) {
    return <AuthLoadingScreen message="Restoring your session…" />
  }

  if (!isAuthenticated) {
    saveAuthReturnUrl(`${location.pathname}${location.search}${location.hash}`)
    return <Navigate to="/sign-in" replace state={{ from: location }} />
  }

  return <Outlet />
}
