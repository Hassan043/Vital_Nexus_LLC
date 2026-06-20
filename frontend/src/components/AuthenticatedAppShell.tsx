import { Link, Outlet } from 'react-router-dom'
import { SignOutButton } from './SignOutButton'
import { useVitalNexusAuth } from '../auth/useVitalNexusAuth'

export function AuthenticatedAppShell() {
  const { account } = useVitalNexusAuth()
  const displayName = account?.name ?? account?.username ?? 'Signed in'

  return (
    <div className="app-layout app-layout-authenticated">
      <header className="site-header session-header">
        <Link to="/" className="brand-link">
          <span className="eyebrow">VitalNexus</span>
        </Link>
        <div className="session-toolbar">
          <div className="session-user" aria-label="Signed-in user">
            <span className="session-status">Signed in</span>
            <span className="session-name">{displayName}</span>
            {account?.username ? <span className="session-email">{account.username}</span> : null}
          </div>
          <SignOutButton />
        </div>
      </header>
      <main className="app-shell">
        <Outlet />
      </main>
    </div>
  )
}
