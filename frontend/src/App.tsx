import { useEffect, useState } from 'react'
import { MsalProvider } from '@azure/msal-react'
import { isMsalConfigured } from './auth/authConfig'
import { initializeMsal } from './auth/msalInstance'
import type { PublicClientApplication } from '@azure/msal-browser'
import { AuthSetupPage } from './pages/AuthSetupPage'
import { HomePage } from './pages/HomePage'

function App() {
  const [msalInstance, setMsalInstance] = useState<PublicClientApplication | null>(null)
  const [bootstrapError, setBootstrapError] = useState<string | null>(null)

  useEffect(() => {
    if (!isMsalConfigured()) {
      return
    }

    initializeMsal()
      .then(setMsalInstance)
      .catch((error: unknown) => {
        const message = error instanceof Error ? error.message : 'Failed to initialize MSAL.'
        setBootstrapError(message)
      })
  }, [])

  if (!isMsalConfigured()) {
    return <AuthSetupPage />
  }

  if (bootstrapError) {
    return (
      <main className="app-shell">
        <section className="auth-panel">
          <h1>Authentication setup error</h1>
          <p className="auth-status">{bootstrapError}</p>
        </section>
      </main>
    )
  }

  if (!msalInstance) {
    return (
      <main className="app-shell">
        <section className="auth-panel">
          <p className="auth-status">Loading authentication…</p>
        </section>
      </main>
    )
  }

  return (
    <MsalProvider instance={msalInstance}>
      <HomePage />
    </MsalProvider>
  )
}

export default App
