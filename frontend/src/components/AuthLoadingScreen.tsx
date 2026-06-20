type AuthLoadingScreenProps = {
  message?: string
}

export function AuthLoadingScreen({ message = 'Loading…' }: AuthLoadingScreenProps) {
  return (
    <main className="app-shell auth-loading-screen" aria-live="polite">
      <section className="auth-panel">
        <p className="auth-status">{message}</p>
      </section>
    </main>
  )
}
