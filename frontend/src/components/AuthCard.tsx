import type { ReactNode } from 'react'

type AuthCardProps = {
  title: string
  subtitle?: string
  eyebrow?: string
  wide?: boolean
  children: ReactNode
}

export function AuthCard({ title, subtitle, eyebrow, wide = false, children }: AuthCardProps) {
  return (
    <div className="auth-page">
      <section
        className={`auth-card${wide ? ' auth-card-wide' : ''}`}
        aria-labelledby="auth-card-title"
      >
        {eyebrow ? <p className="auth-card-eyebrow">{eyebrow}</p> : null}
        <header className="auth-card-header">
          <h1 id="auth-card-title">{title}</h1>
          {subtitle ? <p className="auth-card-subtitle">{subtitle}</p> : null}
        </header>
        <div className="auth-card-body">{children}</div>
      </section>
    </div>
  )
}
