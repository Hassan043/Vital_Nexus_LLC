import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { AppLayout } from '../components/AppLayout'
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
    <AppLayout>
      <div className="flow-header">
        <p className="eyebrow">Sign in</p>
        <h1>Sign in to VitalNexus</h1>
        <p className="lede">Use your clinic account. Password and MFA are handled by Microsoft Entra External ID.</p>
      </div>

      <section className="auth-panel">
        {displayReturnPath && displayReturnPath !== '/' ? (
          <p className="auth-status">Sign in to continue to <strong>{displayReturnPath}</strong>.</p>
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
        <p className="flow-note">If provided, your email is prefilled on the Microsoft sign-in page.</p>
        <div className="auth-actions">
          <button type="button" className="auth-button" disabled={isLoading} onClick={() => void handleSignIn()}>
            {isLoading ? 'Signing in…' : 'Sign in'}
          </button>
          <Link to="/create-account" className="text-link">
            Create an account
          </Link>
        </div>
      </section>
    </AppLayout>
  )
}
