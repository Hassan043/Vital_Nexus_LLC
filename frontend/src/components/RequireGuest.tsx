import type { ReactNode } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { consumeAuthReturnUrl } from '../auth/returnUrl'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'
import { AuthLoadingScreen } from './AuthLoadingScreen'

type RequireGuestProps = {
  children?: ReactNode
}

export function RequireGuest({ children }: RequireGuestProps) {
  const { isAuthenticated, isLoading } = useVitalNexusAuth()

  if (isLoading) {
    return <AuthLoadingScreen message="Loading sign-in…" />
  }

  if (isAuthenticated) {
    return <Navigate to={consumeAuthReturnUrl() ?? '/'} replace />
  }

  if (children) {
    return children
  }

  return <Outlet />
}
