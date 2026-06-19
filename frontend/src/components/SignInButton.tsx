import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

export function SignInButton() {
  const { signIn, isLoading } = useVitalNexusAuth()

  return (
    <button type="button" className="auth-button" onClick={() => void signIn()} disabled={isLoading}>
      {isLoading ? 'Signing in…' : 'Sign in'}
    </button>
  )
}
