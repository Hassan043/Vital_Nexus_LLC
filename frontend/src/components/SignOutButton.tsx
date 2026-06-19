import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

export function SignOutButton() {
  const { signOut, isLoading } = useVitalNexusAuth()

  return (
    <button type="button" className="auth-button auth-button-secondary" onClick={() => void signOut()} disabled={isLoading}>
      {isLoading ? 'Signing out…' : 'Sign out'}
    </button>
  )
}
