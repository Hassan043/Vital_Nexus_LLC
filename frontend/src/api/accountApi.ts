import type { ApiClient } from './apiClient'

export type AccountProfile = {
  userId?: string | null
  customerId?: string | null
  objectId: string | null
  name: string | null
  email: string | null
  scopes: string | null
  roles?: string[]
}

export type AdminAccountOverview = {
  customerId: string
  customerName: string
  userId: string
  email: string
  displayName: string | null
  roles: string[]
  access: string
}

export type OnboardingDashboard = {
  authenticationProvider: string
  authorizationProvider: string
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
    createdAt: string
    activatedAt: string | null
    planTier: string
    planTierDescription: string | null
    monthlyPriceCents?: number
    patientCapMax?: number
  } | null
  patientsDatabase: {
    databaseName: string
    serverName: string | null
    provisionedAt: string
    isActive: boolean
    schemaNote: string
  } | null
  clinics: Array<{
    id: string
    name: string
    isActive: boolean
    createdAt: string
  }>
  users: Array<{
    id: string
    email: string
    displayName: string | null
    accountStatus: string
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

export async function getAdminAccountOverview(api: ApiClient): Promise<AdminAccountOverview> {
  return api.get<AdminAccountOverview>('/api/admin/account')
}

export async function getOnboardingDashboard(api: ApiClient): Promise<OnboardingDashboard> {
  return api.get<OnboardingDashboard>('/api/admin/onboarding')
}

export async function inviteStaffUser(api: ApiClient, email: string): Promise<{ email: string; message: string }> {
  return api.post<{ email: string; message: string }>('/api/admin/users/invite', { email, roleName: 'User' })
}

export async function createClinic(api: ApiClient, name: string): Promise<{ id: string; name: string }> {
  return api.post<{ id: string; name: string }>('/api/admin/clinics', { name })
}
