import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getOnboardingDashboard, type OnboardingDashboard } from '../api/accountApi'
import { createBillingQuote, formatUsd, getBillingPlans, type BillingPlan } from '../api/billingApi'
import { useApiClient } from '../api/useApiClient'
import { useAccountProfile } from '../api/useAccountProfile'
import { ApiError } from '../api/apiClient'
import { isAdmin } from '../auth/roles'

const checklistSteps = [
  { key: 'baaSigned', label: 'Sign Business Associate Agreement (BAA)' },
  { key: 'planSelected', label: 'Select subscription plan' },
  { key: 'clinicProfileComplete', label: 'Complete clinic profile' },
  { key: 'isComplete', label: 'Provision subscription and Patients database' },
] as const

export function OnboardingPage() {
  const api = useApiClient()
  const { profile, loading: profileLoading } = useAccountProfile()
  const [dashboard, setDashboard] = useState<OnboardingDashboard | null>(null)
  const [plans, setPlans] = useState<BillingPlan[]>([])
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(true)
  const [baaAccepted, setBaaAccepted] = useState(false)
  const [selectedPlanId, setSelectedPlanId] = useState(1)
  const [customerName, setCustomerName] = useState('')
  const [clinicName, setClinicName] = useState('')
  const [contactEmail, setContactEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [timeZoneId, setTimeZoneId] = useState('America/New_York')

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const [dashboardResult, planResult] = await Promise.all([
        getOnboardingDashboard(api),
        getBillingPlans(api),
      ])
      setDashboard(dashboardResult)
      setPlans(planResult)
      setCustomerName(dashboardResult.customer.name)
      setContactEmail(dashboardResult.users[0]?.email ?? '')
      if (planResult.length > 0) {
        setSelectedPlanId(planResult[0].id)
      }
    } catch (caught: unknown) {
      setError(caught instanceof ApiError ? caught.body || caught.message : 'Failed to load onboarding.')
    } finally {
      setLoading(false)
    }
  }, [api])

  useEffect(() => {
    void load()
  }, [load])

  const selectedPlan = useMemo(
    () => plans.find((plan) => plan.id === selectedPlanId) ?? null,
    [plans, selectedPlanId],
  )

  if (profileLoading || loading) {
    return <p className="auth-status">Loading onboarding…</p>
  }

  if (!isAdmin(profile?.roles)) {
    return <Navigate to="/" replace />
  }

  if (dashboard?.onboarding.isComplete) {
    return <Navigate to="/" replace />
  }

  async function handleSignBaa() {
    setMessage('')
    setError('')
    if (!baaAccepted) {
      setError('Accept the BAA checkbox to continue.')
      return
    }
    try {
      await api.post('/api/admin/onboarding/baa', {})
      setMessage('BAA recorded.')
      await load()
    } catch (caught: unknown) {
      setError(caught instanceof ApiError ? caught.body || caught.message : 'BAA signing failed.')
    }
  }

  async function handleSelectPlan() {
    setMessage('')
    setError('')
    try {
      const quote = await createBillingQuote(api, selectedPlanId)
      await api.post('/api/admin/onboarding/plan', { planTierId: quote.planTierId })
      setMessage(`Plan selected: ${quote.planName} (${formatUsd(quote.monthlyPriceCents)}/mo, up to ${quote.patientCapMax} patients).`)
      await load()
    } catch (caught: unknown) {
      setError(caught instanceof ApiError ? caught.body || caught.message : 'Plan selection failed.')
    }
  }

  async function handleSaveClinicProfile() {
    setMessage('')
    setError('')
    if (!clinicName.trim() || !contactEmail.trim() || !phone.trim() || !timeZoneId.trim()) {
      setError('Fill in clinic name, contact email, phone, and time zone.')
      return
    }
    try {
      await api.put('/api/admin/onboarding/clinic-profile', {
        customerDisplayName: customerName,
        clinicName,
        contactEmail,
        phone,
        timeZoneId,
      })
      setMessage('Clinic profile saved.')
      await load()
    } catch (caught: unknown) {
      setError(caught instanceof ApiError ? caught.displayMessage : 'Clinic profile update failed.')
    }
  }

  async function handleComplete() {
    setMessage('')
    setError('')
    try {
      await api.post('/api/admin/onboarding/complete', {
        customerDisplayName: customerName,
        clinicName,
        contactEmail,
        phone,
        timeZoneId,
        planTierId: selectedPlanId,
      })
      setMessage('Onboarding complete. Your account is now active.')
      await load()
    } catch (caught: unknown) {
      setError(caught instanceof ApiError ? caught.body || caught.message : 'Onboarding completion failed.')
    }
  }

  const onboarding = dashboard?.onboarding

  return (
    <div className="onboarding-page">
      <div className="flow-header">
        <p className="eyebrow">Customer onboarding</p>
        <h1>Complete your VitalNexus setup</h1>
        <p className="lede">
          One customer, one subscription, one Patients database, multiple clinics, multiple staff logins.
          Authentication is handled by Microsoft Entra External ID.
        </p>
      </div>

      <section className="auth-panel onboarding-checklist">
        <h2>Guided setup checklist</h2>
        <ul className="onboarding-steps">
          {checklistSteps.map((step) => {
            const done = onboarding?.[step.key as keyof typeof onboarding] === true
            return (
              <li key={step.key} className={done ? 'step-complete' : 'step-pending'}>
                {done ? '✓' : '○'} {step.label}
              </li>
            )
          })}
        </ul>
      </section>

      {!onboarding?.baaSigned ? (
        <section className="auth-panel">
          <h2>Business Associate Agreement</h2>
          <p className="flow-note">
            Demo BAA: by checking the box below you agree to VitalNexus handling PHI according to your organization&apos;s BAA terms (version 2026.1).
          </p>
          <label className="checkbox-row">
            <input type="checkbox" checked={baaAccepted} onChange={(event) => setBaaAccepted(event.target.checked)} />
            I agree to the Business Associate Agreement
          </label>
          <button type="button" className="primary-button" onClick={() => void handleSignBaa()}>
            Sign BAA and continue
          </button>
        </section>
      ) : null}

      {onboarding?.baaSigned && !onboarding.planSelected ? (
        <section className="auth-panel">
          <h2>Select plan</h2>
          <p className="flow-note">Pricing is loaded from the server. Client-supplied amounts are rejected.</p>
          <div className="plan-grid">
            {plans.map((plan) => (
              <label key={plan.id} className={`plan-card ${selectedPlanId === plan.id ? 'plan-selected' : ''}`}>
                <input
                  type="radio"
                  name="plan"
                  checked={selectedPlanId === plan.id}
                  onChange={() => setSelectedPlanId(plan.id)}
                />
                <strong>{plan.name}</strong>
                <span>{formatUsd(plan.monthlyPriceCents)}/month</span>
                <span>Up to {plan.patientCapMax} patients</span>
                {plan.description ? <span className="flow-note">{plan.description}</span> : null}
              </label>
            ))}
          </div>
          <button type="button" className="primary-button" onClick={() => void handleSelectPlan()}>
            Continue with {selectedPlan?.name ?? 'plan'}
          </button>
        </section>
      ) : null}

      {onboarding?.baaSigned && onboarding.planSelected && !onboarding.clinicProfileComplete ? (
        <section className="auth-panel">
          <h2>Clinic profile</h2>
          <label className="field-label" htmlFor="customer-name">Customer name</label>
          <input id="customer-name" className="text-input" value={customerName} onChange={(e) => setCustomerName(e.target.value)} />
          <label className="field-label" htmlFor="clinic-name">Clinic name</label>
          <input id="clinic-name" className="text-input" value={clinicName} onChange={(e) => setClinicName(e.target.value)} />
          <label className="field-label" htmlFor="contact-email">Contact email</label>
          <input id="contact-email" className="text-input" value={contactEmail} onChange={(e) => setContactEmail(e.target.value)} />
          <label className="field-label" htmlFor="phone">Phone</label>
          <input id="phone" className="text-input" value={phone} onChange={(e) => setPhone(e.target.value)} />
          <label className="field-label" htmlFor="timezone">Time zone</label>
          <input id="timezone" className="text-input" value={timeZoneId} onChange={(e) => setTimeZoneId(e.target.value)} />
          <button type="button" className="primary-button" onClick={() => void handleSaveClinicProfile()}>
            Save clinic profile
          </button>
        </section>
      ) : null}

      {onboarding?.baaSigned && onboarding.planSelected && onboarding.clinicProfileComplete && !onboarding.isComplete ? (
        <section className="auth-panel">
          <h2>Activate account</h2>
          <p className="flow-note">
            This provisions your subscription, dedicated Patients database (with Placeholder table), and default clinic membership.
          </p>
          <button type="button" className="primary-button" onClick={() => void handleComplete()}>
            Complete onboarding
          </button>
        </section>
      ) : null}

      {error ? <p className="field-error">{error}</p> : null}
      {message ? <p className="auth-status">{message}</p> : null}
    </div>
  )
}
