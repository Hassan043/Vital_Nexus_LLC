import { useState } from 'react'
import {
  createClinic,
  inviteStaffUser,
  type AccountProfile,
  type OnboardingDashboard,
} from '../api/accountApi'
import { formatUsd } from '../api/billingApi'
import { useApiClient } from '../api/useApiClient'
import { ApiError } from '../api/apiClient'
import { isAdmin } from '../auth/roles'
import { DataTable, KeyValueTable, TableSection, type DataTableColumn } from './DataTable'

type WorkspaceDashboardProps = {
  profile: AccountProfile | null
  dashboard: OnboardingDashboard | null
  dashboardLoading: boolean
  dashboardError: string
  onRefresh: () => void
}

function formatTimeZone(timeZoneId: string | null | undefined): string {
  if (!timeZoneId) {
    return '—'
  }

  return timeZoneId.replace(/_/g, ' ')
}

function formatSubscription(subscription: OnboardingDashboard['subscription']): string {
  if (!subscription) {
    return 'No active plan'
  }

  const price =
    subscription.monthlyPriceCents !== undefined
      ? `${formatUsd(subscription.monthlyPriceCents)}/mo`
      : null
  const cap = subscription.patientCapMax !== undefined ? `up to ${subscription.patientCapMax} patients` : null
  const parts = [subscription.planTier, price, cap, subscription.status].filter(Boolean)
  return parts.join(' · ')
}

function formatRole(roles: string[] | null | undefined): string {
  if (!roles?.length) {
    return 'User'
  }

  return roles.join(', ')
}

function AccountOverview({ profile }: { profile: AccountProfile | null }) {
  const memberships = profile?.clinicMemberships ?? []
  const rows = [
    { label: 'Name', value: profile?.name ?? '—' },
    { label: 'Email', value: profile?.email ?? '—' },
    { label: 'Role', value: formatRole(profile?.roles) },
    {
      label: 'Clinic access',
      value:
        memberships.length > 0
          ? memberships.map((membership) => membership.clinicName).join(', ')
          : 'No clinics assigned',
    },
    ...(profile?.activeClinic
      ? [{ label: 'Current clinic', value: profile.activeClinic.clinicName }]
      : []),
  ]

  return (
    <TableSection title="Your account">
      <KeyValueTable rows={rows} caption="Your account" />
    </TableSection>
  )
}

export function WorkspaceDashboard({
  profile,
  dashboard,
  dashboardLoading,
  dashboardError,
  onRefresh,
}: WorkspaceDashboardProps) {
  const api = useApiClient()
  const [inviteEmail, setInviteEmail] = useState('')
  const [clinicName, setClinicName] = useState('')
  const [actionMessage, setActionMessage] = useState('')
  const userIsAdmin = isAdmin(profile?.roles)

  const displayName = profile?.name ?? profile?.email ?? 'there'
  const organizationName = dashboard?.customer.name ?? 'Your organization'
  const clinics = dashboard?.clinics ?? []
  const users = dashboard?.users ?? []
  const pendingInvitations = dashboard?.pendingInvitations ?? []

  async function handleInvite() {
    setActionMessage('')
    try {
      const result = await inviteStaffUser(api, inviteEmail.trim())
      setActionMessage(result.message)
      setInviteEmail('')
      onRefresh()
    } catch (caught: unknown) {
      setActionMessage(caught instanceof ApiError ? caught.displayMessage : 'Invitation failed.')
    }
  }

  async function handleCreateClinic() {
    setActionMessage('')
    try {
      const result = await createClinic(api, clinicName.trim())
      setActionMessage(`Added clinic "${result.name}".`)
      setClinicName('')
      onRefresh()
    } catch (caught: unknown) {
      setActionMessage(caught instanceof ApiError ? caught.displayMessage : 'Could not add clinic.')
    }
  }

  const clinicColumns: DataTableColumn<OnboardingDashboard['clinics'][number]>[] = [
    { key: 'name', header: 'Clinic', render: (row) => row.name },
    { key: 'contact', header: 'Contact email', render: (row) => row.contactEmail ?? '—' },
    { key: 'phone', header: 'Phone', render: (row) => row.phone ?? '—' },
    { key: 'timezone', header: 'Time zone', render: (row) => formatTimeZone(row.timeZoneId) },
    { key: 'status', header: 'Status', render: (row) => (row.isActive ? 'Active' : 'Inactive') },
  ]

  const teamColumns: DataTableColumn<OnboardingDashboard['users'][number]>[] = [
    { key: 'name', header: 'Name', render: (row) => row.displayName ?? '—' },
    { key: 'email', header: 'Email', render: (row) => row.email },
    { key: 'role', header: 'Role', render: (row) => formatRole(row.roles) },
    { key: 'status', header: 'Status', render: (row) => row.accountStatus },
    {
      key: 'signin',
      header: 'Sign-in',
      render: (row) => (row.entraLinked ? 'Ready' : 'Invitation pending'),
    },
  ]

  const invitationColumns: DataTableColumn<OnboardingDashboard['pendingInvitations'][number]>[] = [
    { key: 'email', header: 'Email', render: (row) => row.email },
    { key: 'role', header: 'Role', render: (row) => row.roleName },
  ]

  if (!userIsAdmin) {
    return (
      <div className="table-page-sections" aria-live="polite">
        <header className="workspace-hero">
          <h1>Welcome, {displayName}</h1>
          <p className="workspace-hero-subtitle">
            VitalNexus lab intelligence for your clinic team.
          </p>
        </header>

        <AccountOverview profile={profile} />

        <TableSection title="Your clinics">
          <DataTable
            columns={[
              { key: 'name', header: 'Clinic', render: (row) => row.clinicName },
              { key: 'status', header: 'Status', render: (row) => (row.isActive ? 'Active' : 'Inactive') },
            ]}
            rows={profile?.clinicMemberships ?? []}
            rowKey={(row) => row.clinicId}
            caption="Your clinics"
            emptyMessage="No clinic access assigned yet. Ask your organization admin for an invitation."
          />
        </TableSection>
      </div>
    )
  }

  return (
    <div className="table-page-sections" aria-live="polite">
      <header className="workspace-hero">
        <h1>{organizationName}</h1>
        <p className="workspace-hero-subtitle">
          Signed in as {displayName}
          {dashboard?.subscription ? ` · ${formatSubscription(dashboard.subscription)}` : null}
        </p>
      </header>

      <AccountOverview profile={profile} />

      <div className="workspace-summary">
        <div className="workspace-stat">
          <span className="workspace-stat-value">{dashboardLoading ? '…' : clinics.length}</span>
          <span className="workspace-stat-label">Clinics</span>
        </div>
        <div className="workspace-stat">
          <span className="workspace-stat-value">{dashboardLoading ? '…' : users.length}</span>
          <span className="workspace-stat-label">Team members</span>
        </div>
        <div className="workspace-stat">
          <span className="workspace-stat-value">{dashboardLoading ? '…' : pendingInvitations.length}</span>
          <span className="workspace-stat-label">Pending invites</span>
        </div>
        <div className="workspace-stat">
          <span className="workspace-stat-value">
            {dashboardLoading ? '…' : dashboard?.patientsDatabase?.isActive ? 'Active' : 'Pending'}
          </span>
          <span className="workspace-stat-label">Patient records</span>
        </div>
      </div>

      {dashboardLoading ? <p className="table-message">Loading organization data…</p> : null}

      {dashboardError ? (
        <div className="table-message table-message-error workspace-error">
          <p>{dashboardError}</p>
          <button type="button" className="table-button" onClick={onRefresh}>
            Retry
          </button>
        </div>
      ) : null}

      <TableSection title="Clinics">
        <DataTable
          columns={clinicColumns}
          rows={clinics}
          rowKey={(row) => row.id}
          caption="Clinics"
          emptyMessage={
            dashboardLoading
              ? 'Loading clinics…'
              : 'No clinics yet. Add your first location below.'
          }
        />
        <table className="data-table data-table-form">
          <tbody>
            <tr>
              <th scope="row">
                <label htmlFor="new-clinic-name">Add clinic</label>
              </th>
              <td>
                <input
                  id="new-clinic-name"
                  className="table-input"
                  value={clinicName}
                  onChange={(event) => setClinicName(event.target.value)}
                  placeholder="e.g. Westside office"
                />
              </td>
              <td className="table-actions">
                <button
                  type="button"
                  className="table-button"
                  disabled={!clinicName.trim()}
                  onClick={() => void handleCreateClinic()}
                >
                  Add clinic
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </TableSection>

      <TableSection title="Team">
        <DataTable
          columns={teamColumns}
          rows={users}
          rowKey={(row) => row.id}
          caption="Team members"
          emptyMessage={dashboardLoading ? 'Loading team…' : 'No team members yet.'}
        />
      </TableSection>

      {pendingInvitations.length > 0 ? (
        <TableSection title="Pending invitations">
          <DataTable
            columns={invitationColumns}
            rows={pendingInvitations}
            rowKey={(row) => row.id}
            caption="Pending invitations"
          />
        </TableSection>
      ) : null}

      <TableSection title="Invite team member" description="Staff sign in with Microsoft Entra using the invited email.">
        <table className="data-table data-table-form">
          <tbody>
            <tr>
              <th scope="row">
                <label htmlFor="invite-email">Work email</label>
              </th>
              <td>
                <input
                  id="invite-email"
                  className="table-input"
                  type="email"
                  value={inviteEmail}
                  onChange={(event) => setInviteEmail(event.target.value)}
                  placeholder="colleague@clinic.com"
                />
              </td>
              <td className="table-actions">
                <button
                  type="button"
                  className="table-button"
                  disabled={!inviteEmail.trim()}
                  onClick={() => void handleInvite()}
                >
                  Send invitation
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </TableSection>

      {actionMessage ? <p className="table-message table-message-success">{actionMessage}</p> : null}
    </div>
  )
}
