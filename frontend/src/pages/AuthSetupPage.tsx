export function AuthSetupPage() {
  return (
    <main className="app-shell">
      <header className="app-header">
        <div>
          <p className="eyebrow">VitalNexus</p>
          <h1>Configure Entra External ID</h1>
          <p className="lede">
            Copy <code>frontend/.env.example</code> to <code>frontend/.env.development.local</code>{' '}
            and set the values from your dev external tenant.
          </p>
        </div>
      </header>

      <section className="auth-panel">
        <p className="auth-status">Required variables:</p>
        <ul className="setup-list">
          <li>
            <code>VITE_B2C_CLIENT_ID</code> — VitalNexus Frontend Dev app ID
          </li>
          <li>
            <code>VITE_B2C_TENANT_ID</code> — external tenant GUID
          </li>
          <li>
            <code>VITE_B2C_TENANT_DOMAIN_PREFIX</code> — e.g. <code>vitalnexusexternal</code>
          </li>
          <li>
            <code>VITE_B2C_API_SCOPE</code> — API delegated scope URI
          </li>
        </ul>
      </section>
    </main>
  )
}
