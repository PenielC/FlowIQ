import { api } from './api'
import type { ApiResponse } from './types'

/** Which part of FinFlow a path belongs to, for usage counts. Null: not counted (admin, setup steps). */
export function featureForPath(pathname: string): string | null {
  if (pathname.startsWith('/transactions/import')) return 'import'
  const first = pathname.split('/')[1]
  const pages = ['dashboard', 'transactions', 'invoices', 'customers', 'forecasting', 'reports', 'subscriptions', 'settings']
  return pages.includes(first) ? first : null
}

const lastSent = new Map<string, number>()

/** Counts a page visit. Fire-and-forget; the same page within a minute counts once (reloads, double renders). */
export function trackPage(feature: string) {
  const now = Date.now()
  if (now - (lastSent.get(feature) ?? 0) < 60_000) return
  lastSent.set(feature, now)
  api.post('/api/usage', { feature }).catch(() => {})
}

// ---- platform admin

export type UsageKind = 'Page' | 'Action'

export interface FeatureUsage {
  key: string
  label: string
  kind: UsageKind
  companies7: number
  companies30: number
  users7: number
  users30: number
  uses30: number
  /** Businesses active in each of the last 12 weeks, oldest first. */
  weeklyCompanies: number[]
}

export interface UsageOverview {
  activeCompanies7: number
  activeCompanies30: number
  /** First day of each of the 12 weeks, oldest first (yyyy-mm-dd). */
  weekStarts: string[]
  features: FeatureUsage[]
  switches: { label: string; companies: number }[]
}

export interface CompanyUsage {
  lastActiveUtc: string | null
  activeUsers30: number
  features: { key: string; label: string; kind: UsageKind; lastUsedUtc: string | null; daysActive30: number; uses30: number }[]
}

export async function fetchUsageOverview() {
  const res = await api.get<ApiResponse<UsageOverview>>('/api/admin/usage')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load usage')
  return res.data.data
}

export async function fetchCompanyUsage(companyId: string) {
  const res = await api.get<ApiResponse<CompanyUsage>>(`/api/admin/companies/${companyId}/usage`)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load usage')
  return res.data.data
}

/** "Today", "3 days ago", a date, or "Never". */
export function sinceText(iso: string | null) {
  if (!iso) return 'Never'
  const days = Math.floor((Date.now() - new Date(iso).getTime()) / 86400000)
  if (days <= 0) return 'Today'
  if (days === 1) return 'Yesterday'
  if (days < 30) return `${days} days ago`
  return new Date(iso).toLocaleDateString('en-US', { day: 'numeric', month: 'short', year: 'numeric' })
}
