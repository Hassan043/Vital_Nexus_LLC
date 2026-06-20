import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

type RequireGuestProps = {
  children: ReactNode
}

export function RequireGuest({ children }: RequireGuestProps) {
  const { isAuthenticated, isLoading } = useVitalNexusAuth()

  if (isLoading) {
    return <p className="auth-status">Loading…</p>
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  return children
}
