import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { AppLayout } from '../components/AppLayout'
import { AuthCard } from '../components/AuthCard'
import { cleanAuthRedirectFromBrowserUrl, formatReturnUrlForDisplay } from '../auth/returnUrl'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function getReturnPath(from: unknown): string | null {
  if (!from || typeof from !== 'object' || !('pathname' in from)) {
    return null
  }

  const location = from as { pathname?: string; search?: string }
  if (!location.pathname) {
    return null
  }

  return `${location.pathname}${location.search ?? ''}`
}

export function SignInPage() {
  const { signIn, isLoading } = useVitalNexusAuth()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const returnPath = getReturnPath(location.state?.from)
  const displayReturnPath = returnPath ? formatReturnUrlForDisplay(returnPath) : null

  useEffect(() => {
    cleanAuthRedirectFromBrowserUrl()
  }, [])

  async function handleSignIn() {
    const trimmedEmail = email.trim()
    await signIn(trimmedEmail && emailPattern.test(trimmedEmail) ? trimmedEmail : undefined)
  }

  return (
    <AppLayout variant="auth">
      <AuthCard
        eyebrow="Sign in"
        title="Welcome back"
        subtitle="Use your clinic account. Password and MFA are handled by Microsoft Entra External ID."
      >
        {displayReturnPath && displayReturnPath !== '/' ? (
          <p className="auth-card-notice">
            Sign in to continue to <strong>{displayReturnPath}</strong>.
          </p>
        ) : null}
        <label className="field-label" htmlFor="signin-email">
          Email address (optional)
        </label>
        <input
          id="signin-email"
          className="field-input"
          type="email"
          autoComplete="username"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          placeholder="you@clinic.com"
        />
        <p className="auth-card-hint">If provided, your email is prefilled on the Microsoft sign-in page.</p>
        <div className="auth-card-actions">
          <button type="button" className="auth-button auth-button-block" disabled={isLoading} onClick={() => void handleSignIn()}>
            {isLoading ? 'Signing in…' : 'Sign in'}
          </button>
          <p className="auth-card-footer">
            New to VitalNexus?{' '}
            <Link to="/create-account" className="text-link">
              Create an account
            </Link>
          </p>
        </div>
      </AuthCard>
    </AppLayout>
  )
}
