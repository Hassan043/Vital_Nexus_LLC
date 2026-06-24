import { useCallback, useEffect, useState } from 'react'
import {
  createClinic,
  getOnboardingDashboard,
  inviteStaffUser,
  type OnboardingDashboard,
} from '../api/accountApi'
import { useApiClient } from '../api/useApiClient'
import { ApiError } from '../api/apiClient'
import { isAdmin } from '../auth/roles'

type CustomerOnboardingDemoProps = {
  roles: string[] | null | undefined
}

const onboardingSteps = [
  { key: 'entraIdentityLinked', label: 'Microsoft Entra External ID registration', detail: 'Authentication — who you are' },
  { key: 'customerCreated', label: 'Customer record created', detail: 'One customer = one subscription = one Patients database' },
  { key: 'baaSigned', label: 'Business Associate Agreement signed', detail: 'Required before account activation' },
  { key: 'planSelected', label: 'Subscription plan selected', detail: 'Server-authoritative pricing' },
  { key: 'adminAssigned', label: 'Admin user assigned (one per customer)', detail: 'Authorization — what you can do' },
  { key: 'subscriptionCreated', label: 'Subscription activated', detail: 'Demo plan tier assigned' },
  { key: 'patientsDatabaseProvisioned', label: 'Dedicated Patients database provisioned', detail: 'Includes Placeholder table for demo' },
  { key: 'defaultClinicCreated', label: 'Default clinic created', detail: 'Customer may add more clinics later' },
  { key: 'accountActivated', label: 'Account activated', detail: 'Full customer workspace enabled' },
] as const

export function CustomerOnboardingDemo({ roles }: CustomerOnboardingDemoProps) {
  const api = useApiClient()
  const [dashboard, setDashboard] = useState<OnboardingDashboard | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [inviteEmail, setInviteEmail] = useState('')
  const [clinicName, setClinicName] = useState('')
  const [actionMessage, setActionMessage] = useState('')
  const canAccess = isAdmin(roles)

  const loadDashboard = useCallback(async () => {
    if (!canAccess) {
      return
    }

    setLoading(true)
    setError('')
    try {
      const result = await getOnboardingDashboard(api)
      setDashboard(result)
    } catch (caught: unknown) {
      if (caught instanceof ApiError) {
        setError(`API ${caught.status}: ${caught.body || caught.message}`)
      } else {
        setError(caught instanceof Error ? caught.message : 'Failed to load onboarding dashboard.')
      }
    } finally {
      setLoading(false)
    }
  }, [api, canAccess])

  useEffect(() => {
    void loadDashboard()
  }, [loadDashboard])

  async function handleInvite() {
    setActionMessage('')
    try {
      const result = await inviteStaffUser(api, inviteEmail.trim())
      setActionMessage(result.message)
      setInviteEmail('')
      await loadDashboard()
    } catch (caught: unknown) {
      setActionMessage(caught instanceof ApiError ? caught.body || caught.message : 'Invite failed.')
    }
  }

  async function handleCreateClinic() {
    setActionMessage('')
    try {
      const result = await createClinic(api, clinicName.trim())
      setActionMessage(`Clinic "${result.name}" created.`)
      setClinicName('')
      await loadDashboard()
    } catch (caught: unknown) {
      setActionMessage(caught instanceof ApiError ? caught.body || caught.message : 'Clinic creation failed.')
    }
  }

  if (!canAccess) {
    return null
  }

  return (
    <section className="auth-panel admin-panel onboarding-demo" aria-live="polite">
      <p className="welcome-label">Customer onboarding demo</p>
      <p className="auth-status">
        <strong>Authentication</strong> is handled by {dashboard?.authenticationProvider ?? 'Microsoft Entra External ID'}.
        {' '}<strong>Authorization</strong> (roles, clinics, database access) is enforced by {dashboard?.authorizationProvider ?? 'VitalNexus'}.
      </p>

      {loading ? <p className="auth-status">Loading onboarding status…</p> : null}
      {error ? <p className="field-error">{error}</p> : null}

      {dashboard ? (
        <>
          <div className="onboarding-grid">
            <article className="onboarding-card">
              <h2>Customer</h2>
              <p className="welcome-name">{dashboard.customer.name}</p>
              <p className="welcome-email">ID: {dashboard.customer.id}</p>
              {dashboard.subscription ? (
                <>
                  <p className="welcome-email">Plan: {dashboard.subscription.planTier}</p>
                  <p className="welcome-email">Status: {dashboard.subscription.status}</p>
                </>
              ) : null}
            </article>

            <article className="onboarding-card">
              <h2>Patients database</h2>
              {dashboard.patientsDatabase ? (
                <>
                  <p className="welcome-email"><code>{dashboard.patientsDatabase.databaseName}</code></p>
                  {dashboard.patientsDatabase.serverName ? (
                    <p className="welcome-email">Server: {dashboard.patientsDatabase.serverName}</p>
                  ) : null}
                  <p className="flow-note">{dashboard.patientsDatabase.schemaNote}</p>
                </>
              ) : (
                <p className="auth-status">Not provisioned yet.</p>
              )}
            </article>
          </div>

          <ol className="onboarding-pipeline">
            {onboardingSteps.map((step) => {
              const complete = dashboard.onboarding[step.key]
              return (
                <li key={step.key} className={complete ? 'is-complete' : 'is-pending'}>
                  <span className="step-number">{complete ? '✓' : '…'}</span>
                  <div>
                    <p className="welcome-name">{step.label}</p>
                    <p className="flow-note">{step.detail}</p>
                  </div>
                </li>
              )
            })}
          </ol>

          <div className="onboarding-grid">
            <article className="onboarding-card">
              <h2>Clinics ({dashboard.clinics.length})</h2>
              <ul className="flow-list">
                {dashboard.clinics.map((clinic) => (
                  <li key={clinic.id}>{clinic.name}</li>
                ))}
              </ul>
              <label className="field-label" htmlFor="new-clinic-name">Add clinic</label>
              <input
                id="new-clinic-name"
                className="field-input"
                value={clinicName}
                onChange={(event) => setClinicName(event.target.value)}
                placeholder="Satellite clinic name"
              />
              <button type="button" className="auth-button" disabled={!clinicName.trim()} onClick={() => void handleCreateClinic()}>
                Create clinic
              </button>
            </article>

            <article className="onboarding-card">
              <h2>Staff users ({dashboard.users.length})</h2>
              <ul className="flow-list">
                {dashboard.users.map((user) => (
                  <li key={user.id}>
                    {user.email} — {user.accountStatus}
                    {user.entraLinked ? ' (Entra linked)' : ' (pending Entra sign-in)'}
                  </li>
                ))}
              </ul>
              {dashboard.pendingInvitations.length > 0 ? (
                <>
                  <p className="welcome-label">Pending invitations</p>
                  <ul className="flow-list">
                    {dashboard.pendingInvitations.map((invitation) => (
                      <li key={invitation.id}>{invitation.email} ({invitation.roleName})</li>
                    ))}
                  </ul>
                </>
              ) : null}
              <label className="field-label" htmlFor="invite-email">Invite staff (User role)</label>
              <input
                id="invite-email"
                className="field-input"
                type="email"
                value={inviteEmail}
                onChange={(event) => setInviteEmail(event.target.value)}
                placeholder="colleague@clinic.com"
              />
              <p className="flow-note">They register via Microsoft Entra External ID with this email. VitalNexus assigns the User role.</p>
              <button type="button" className="auth-button" disabled={!inviteEmail.trim()} onClick={() => void handleInvite()}>
                Send invitation
              </button>
            </article>
          </div>
        </>
      ) : null}

      {actionMessage ? <p className="auth-status">{actionMessage}</p> : null}
    </section>
  )
}
