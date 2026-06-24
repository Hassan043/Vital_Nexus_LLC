import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

type AppLayoutProps = {
  children: ReactNode
  showAuthLinks?: boolean
  variant?: 'default' | 'auth'
}

export function AppLayout({ children, showAuthLinks = true, variant = 'default' }: AppLayoutProps) {
  return (
    <div className={`app-layout${variant === 'auth' ? ' app-layout-auth' : ''}`}>
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
      <main className={variant === 'auth' ? 'app-shell app-shell-auth' : 'app-shell'}>{children}</main>
    </div>
  )
}
