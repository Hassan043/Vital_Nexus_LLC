import type { ApiClient } from './apiClient'

export type ClinicMembership = {
  clinicId: string
  clinicName: string
  joinedAt: string
  isActive: boolean
}

export type AccountProfile = {
  userId?: string | null
  customerId?: string | null
  objectId: string | null
  name: string | null
  email: string | null
  scopes: string | null
  roles?: string[]
  clinicMemberships?: ClinicMembership[]
  activeClinic?: {
    clinicId: string
    clinicName: string
  } | null
}

export type OnboardingDashboard = {
  customer: {
    id: string
    name: string
    createdAt: string
  }
  onboarding: {
    customerCreated: boolean
    entraIdentityLinked: boolean
    baaSigned: boolean
    planSelected: boolean
    clinicProfileComplete: boolean
    subscriptionCreated: boolean
    patientsDatabaseProvisioned: boolean
    defaultClinicCreated: boolean
    adminAssigned: boolean
    accountActivated: boolean
    isComplete: boolean
  }
  subscription: {
    status: string
    planTier: string
    planTierDescription: string | null
    monthlyPriceCents?: number
    patientCapMax?: number
  } | null
  patientsDatabase: {
    isActive: boolean
  } | null
  clinics: Array<{
    id: string
    name: string
    isActive: boolean
    contactEmail: string | null
    phone: string | null
    timeZoneId: string | null
  }>
  users: Array<{
    id: string
    email: string
    displayName: string | null
    accountStatus: string
    roles: string[]
    entraLinked: boolean
  }>
  pendingInvitations: Array<{
    id: string
    email: string
    roleName: string
    createdAt: string
  }>
}

export async function getCurrentAccount(api: ApiClient): Promise<AccountProfile> {
  return api.get<AccountProfile>('/api/me')
}

export async function getWorkspaceDashboard(api: ApiClient): Promise<OnboardingDashboard> {
  return api.get<OnboardingDashboard>('/api/admin/onboarding')
}

export async function inviteStaffUser(api: ApiClient, email: string): Promise<{ email: string; message: string }> {
  return api.post<{ email: string; message: string }>('/api/admin/users/invite', { email, roleName: 'User' })
}

export async function createClinic(api: ApiClient, name: string): Promise<{ id: string; name: string }> {
  return api.post<{ id: string; name: string }>('/api/admin/clinics', { name })
}

export function isOnboardingComplete(
  onboarding: OnboardingDashboard['onboarding'] | null | undefined,
): boolean {
  return onboarding?.isComplete === true
}
