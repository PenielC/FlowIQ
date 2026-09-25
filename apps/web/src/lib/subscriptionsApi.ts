import { api } from './api'
import type { ApiResponse, SubscriptionPlanResponse, SubscriptionStatusResponse } from './types'

export async function fetchSubscriptionPlans() {
  const res = await api.get<ApiResponse<SubscriptionPlanResponse[]>>('/api/subscriptions/plans')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load plans')
  return res.data.data
}

export async function fetchSubscriptionStatus() {
  const res = await api.get<ApiResponse<SubscriptionStatusResponse>>('/api/subscriptions/status')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load subscription status')
  return res.data.data
}

export async function createCheckoutSession(planKey: string) {
  const res = await api.post<ApiResponse<{ checkoutUrl: string }>>('/api/subscriptions/checkout-session', { planKey })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to start checkout')
  return res.data.data.checkoutUrl
}

export async function createBillingPortalSession() {
  const res = await api.post<ApiResponse<{ portalUrl: string }>>('/api/subscriptions/billing-portal-session')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to open billing portal')
  return res.data.data.portalUrl
}
