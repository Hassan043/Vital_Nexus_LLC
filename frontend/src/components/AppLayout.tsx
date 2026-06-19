import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

type AppLayoutProps = {
  children: ReactNode
  showAuthLinks?: boolean
}

export function AppLayout({ children, showAuthLinks = true }: AppLayoutProps) {
  return (
    <div className="app-layout">
      <header className="site-header">
        <Link to="/" className="brand-link">
          <span className="eyebrow">VitalNexus</span>
        </Link>
        {showAuthLinks ? (
          <nav className="site-nav" aria-label="Account">
            <Link to="/sign-in">Sign in</Link>
            <Link to="/create-account" className="site-nav-primary">
              Create account
            </Link>
          </nav>
        ) : null}
      </header>
      <main className="app-shell">{children}</main>
    </div>
  )
}
