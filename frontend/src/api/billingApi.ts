import type { ApiClient } from './apiClient'

export type BillingPlan = {
  id: number
  name: string
  description: string | null
  monthlyPriceCents: number
  patientCapMax: number
  currency: string
}

export type BillingQuote = {
  planTierId: number
  planName: string
  monthlyPriceCents: number
  patientCapMax: number
  currency: string
}

export async function getBillingPlans(api: ApiClient): Promise<BillingPlan[]> {
  return api.get<BillingPlan[]>('/api/billing/plans')
}

export async function getBillingPlansPublic(): Promise<BillingPlan[]> {
  const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5158'
  const response = await fetch(`${baseUrl}/api/billing/plans`)
  if (!response.ok) {
    throw new Error('Failed to load billing plans.')
  }
  return response.json() as Promise<BillingPlan[]>
}

export async function createBillingQuote(
  api: ApiClient,
  planTierId: number,
  clientMonthlyPriceCents?: number,
): Promise<BillingQuote> {
  return api.post<BillingQuote>('/api/billing/quote', {
    planTierId,
    clientMonthlyPriceCents,
  })
}

export function formatUsd(cents: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(cents / 100)
}
