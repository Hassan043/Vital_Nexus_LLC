import { SignInButton } from '../components/SignInButton'
import { SignOutButton } from '../components/SignOutButton'
import { WelcomeUser } from '../components/WelcomeUser'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

export function HomePage() {
  const { account, isAuthenticated, isLoading } = useVitalNexusAuth()

  return (
    <main className="app-shell">
      <header className="app-header">
        <div>
          <p className="eyebrow">VitalNexus</p>
          <h1>Functional medicine lab intelligence</h1>
          <p className="lede">
            Customer sign-in is handled by Microsoft Entra External ID. VitalNexus does not store
            passwords or MFA secrets.
          </p>
        </div>
      </header>

      <section className="auth-panel" aria-live="polite">
        {isLoading ? (
          <p className="auth-status">Completing sign-in…</p>
        ) : isAuthenticated && account ? (
          <>
            <WelcomeUser account={account} />
            <SignOutButton />
          </>
        ) : (
          <>
            <p className="auth-status">Sign in to continue to your clinic workspace.</p>
            <SignInButton />
          </>
        )}
      </section>
    </main>
  )
}
