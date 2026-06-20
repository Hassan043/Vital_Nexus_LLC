import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

type RequireAuthProps = {
  children: ReactNode
}

export function RequireAuth({ children }: RequireAuthProps) {
  const { isAuthenticated, isLoading } = useVitalNexusAuth()

  if (isLoading) {
    return <p className="auth-status">Loading…</p>
  }

  if (!isAuthenticated) {
    return <Navigate to="/sign-in" replace />
  }

  return children
}
