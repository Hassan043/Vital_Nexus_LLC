import { useCallback, useEffect, useMemo, useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { getWorkspaceDashboard, isOnboardingComplete, type OnboardingDashboard } from '../api/accountApi'
import { createBillingQuote, formatUsd, getBillingPlans, type BillingPlan } from '../api/billingApi'
import { useApiClient } from '../api/useApiClient'
import { useAccountProfile } from '../api/useAccountProfile'
import { ApiError } from '../api/apiClient'
import { isAdmin } from '../auth/roles'
import { DataTable, StatusCell, TableSection, type DataTableColumn } from '../components/DataTable'

const checklistSteps = [
  { key: 'baaSigned', label: 'Sign Business Associate Agreement (BAA)' },
  { key: 'planSelected', label: 'Select subscription plan' },
  { key: 'clinicProfileComplete', label: 'Complete clinic profile' },
  { key: 'isComplete', label: 'Provision subscription and Patients database' },
] as const

type ChecklistRow = {
  key: string
  step: string
  complete: boolean
}

export function OnboardingPage() {
  const api = useApiClient()
  const navigate = useNavigate()
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
        getWorkspaceDashboard(api),
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
    return <p className="table-message">Loading onboarding…</p>
  }

  if (!isAdmin(profile?.roles)) {
    return <Navigate to="/" replace />
  }

  if (isOnboardingComplete(dashboard?.onboarding)) {
    return <Navigate to="/" replace />
  }

  if (!dashboard) {
    return (
      <div className="table-page">
        <p className="table-message table-message-error">{error || 'Could not load onboarding status.'}</p>
        <button type="button" className="table-button" onClick={() => void load()}>
          Retry
        </button>
      </div>
    )
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
      const dashboardResult = await getWorkspaceDashboard(api)
      setDashboard(dashboardResult)
      if (isOnboardingComplete(dashboardResult.onboarding)) {
        navigate('/', { replace: true })
        return
      }
      setMessage('Onboarding complete. Your account is now active.')
    } catch (caught: unknown) {
      setError(caught instanceof ApiError ? caught.body || caught.message : 'Onboarding completion failed.')
    }
  }

  const onboarding = dashboard?.onboarding

  const checklistRows: ChecklistRow[] = checklistSteps.map((step) => ({
    key: step.key,
    step: step.label,
    complete: onboarding?.[step.key as keyof typeof onboarding] === true,
  }))

  const checklistColumns: DataTableColumn<ChecklistRow>[] = [
    {
      key: 'status',
      header: 'Status',
      align: 'center',
      render: (row) => <StatusCell complete={row.complete} />,
    },
    { key: 'step', header: 'Step', render: (row) => row.step },
  ]

  const planColumns: DataTableColumn<BillingPlan>[] = [
    {
      key: 'select',
      header: 'Select',
      align: 'center',
      render: (plan) => (
        <input
          type="radio"
          name="plan"
          checked={selectedPlanId === plan.id}
          onChange={() => setSelectedPlanId(plan.id)}
          aria-label={`Select ${plan.name}`}
        />
      ),
    },
    { key: 'name', header: 'Plan', render: (plan) => plan.name },
    { key: 'price', header: 'Monthly price', render: (plan) => formatUsd(plan.monthlyPriceCents) },
    { key: 'cap', header: 'Patient cap', render: (plan) => plan.patientCapMax },
    { key: 'description', header: 'Description', render: (plan) => plan.description ?? '—' },
  ]

  return (
    <div className="table-page">
      <header className="page-header">
        <h1>Customer onboarding</h1>
        <p className="page-subtitle">
          One customer, one subscription, one Patients database — multiple clinics and staff logins.
        </p>
      </header>

      <TableSection title="Setup checklist">
        <DataTable
          columns={checklistColumns}
          rows={checklistRows}
          rowKey={(row) => row.key}
          caption="Setup checklist"
        />
      </TableSection>

      {!onboarding?.baaSigned ? (
        <TableSection title="Business Associate Agreement">
          <table className="data-table data-table-form">
            <tbody>
              <tr>
                <th scope="row">Terms</th>
                <td>
                  Demo BAA version 2026.1 — VitalNexus handling PHI per your organization&apos;s BAA terms.
                </td>
              </tr>
              <tr>
                <th scope="row">Acceptance</th>
                <td>
                  <label className="table-checkbox">
                    <input
                      type="checkbox"
                      checked={baaAccepted}
                      onChange={(event) => setBaaAccepted(event.target.checked)}
                    />
                    I agree to the Business Associate Agreement
                  </label>
                </td>
              </tr>
              <tr>
                <th scope="row">Action</th>
                <td className="table-actions">
                  <button type="button" className="table-button" onClick={() => void handleSignBaa()}>
                    Sign BAA and continue
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </TableSection>
      ) : null}

      {onboarding?.baaSigned && !onboarding.planSelected ? (
        <TableSection title="Select plan" description="Pricing is loaded from the server.">
          <DataTable
            columns={planColumns}
            rows={plans}
            rowKey={(plan) => String(plan.id)}
            caption="Subscription plans"
          />
          <div className="table-toolbar">
            <button type="button" className="table-button" onClick={() => void handleSelectPlan()}>
              Continue with {selectedPlan?.name ?? 'plan'}
            </button>
          </div>
        </TableSection>
      ) : null}

      {onboarding?.baaSigned && onboarding.planSelected && !onboarding.clinicProfileComplete ? (
        <TableSection title="Clinic profile">
          <table className="data-table data-table-form">
            <tbody>
              <tr>
                <th scope="row">
                  <label htmlFor="customer-name">Customer name</label>
                </th>
                <td>
                  <input
                    id="customer-name"
                    className="table-input"
                    value={customerName}
                    onChange={(e) => setCustomerName(e.target.value)}
                  />
                </td>
              </tr>
              <tr>
                <th scope="row">
                  <label htmlFor="clinic-name">Clinic name</label>
                </th>
                <td>
                  <input
                    id="clinic-name"
                    className="table-input"
                    value={clinicName}
                    onChange={(e) => setClinicName(e.target.value)}
                  />
                </td>
              </tr>
              <tr>
                <th scope="row">
                  <label htmlFor="contact-email">Contact email</label>
                </th>
                <td>
                  <input
                    id="contact-email"
                    className="table-input"
                    value={contactEmail}
                    onChange={(e) => setContactEmail(e.target.value)}
                  />
                </td>
              </tr>
              <tr>
                <th scope="row">
                  <label htmlFor="phone">Phone</label>
                </th>
                <td>
                  <input id="phone" className="table-input" value={phone} onChange={(e) => setPhone(e.target.value)} />
                </td>
              </tr>
              <tr>
                <th scope="row">
                  <label htmlFor="timezone">Time zone</label>
                </th>
                <td>
                  <input
                    id="timezone"
                    className="table-input"
                    value={timeZoneId}
                    onChange={(e) => setTimeZoneId(e.target.value)}
                  />
                </td>
              </tr>
              <tr>
                <th scope="row">Action</th>
                <td className="table-actions">
                  <button type="button" className="table-button" onClick={() => void handleSaveClinicProfile()}>
                    Save clinic profile
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </TableSection>
      ) : null}

      {onboarding?.baaSigned && onboarding.planSelected && onboarding.clinicProfileComplete && !onboarding.isComplete ? (
        <TableSection title="Activate account">
          <table className="data-table data-table-form">
            <tbody>
              <tr>
                <th scope="row">Provisioning</th>
                <td>
                  Activates subscription, dedicated Patients database (with Placeholder table), and default clinic
                  membership.
                </td>
              </tr>
              <tr>
                <th scope="row">Action</th>
                <td className="table-actions">
                  <button type="button" className="table-button" onClick={() => void handleComplete()}>
                    Complete onboarding
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </TableSection>
      ) : null}

      {error ? <p className="table-message table-message-error">{error}</p> : null}
      {message ? <p className="table-message">{message}</p> : null}
    </div>
  )
}
