import { subscriptionStatusColor, subscriptionStatusLabel } from '../../lib/subscriptionStatusDisplay'
import type { AdminOverviewResponse } from '../../lib/types'

const STATUS_DISPLAY_ORDER = ['Active', 'Trialing', 'PastDue', 'Unpaid', 'Canceled', 'Incomplete', 'NoSubscription']

function SectionLabel({ children }: { children: string }) {
  return <span className="text-[11px] font-semibold uppercase tracking-wider text-slate-400">{children}</span>
}

function Metric({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-sm text-slate-500">{label}</span>
      <span className={`text-[28px] font-bold leading-none ${highlight ? 'text-brand-primary' : 'text-slate-900'}`}>{value}</span>
    </div>
  )
}

export function AdminReportPanel({ overview, isLoading }: { overview: AdminOverviewResponse | null; isLoading: boolean }) {
  const v = (n: number | undefined) => (isLoading ? '…' : String(n ?? 0))

  const statusEntries = overview
    ? STATUS_DISPLAY_ORDER
        .filter((status) => (overview.statusCounts[status] ?? 0) > 0)
        .map((status) => [status, overview.statusCounts[status]] as const)
    : []

  const webCount = overview?.usageByPlatform.web ?? 0
  const mobileCount = overview?.usageByPlatform.mobile ?? 0
  const totalActive = webCount + mobileCount
  const webPercent = totalActive === 0 ? 0 : (webCount / totalActive) * 100

  return (
    <div className="rounded-2xl border border-slate-200 bg-white px-6">
      <div className="flex flex-col gap-3 border-b border-slate-100 py-5">
        <SectionLabel>Companies</SectionLabel>
        <div className="flex gap-14">
          <Metric label="Total Companies" value={v(overview?.totalCompanies)} />
          <Metric label="New This Month" value={v(overview?.newCompaniesThisMonth)} highlight />
        </div>
      </div>

      <div className="flex flex-col gap-3 border-b border-slate-100 py-5">
        <SectionLabel>Subscriptions</SectionLabel>
        <div className="flex gap-14">
          <Metric label="Total Subscriptions" value={v(overview?.totalSubscriptions)} />
          <Metric label="New This Month" value={v(overview?.newSubscriptionsThisMonth)} highlight />
        </div>
        {statusEntries.length > 0 && (
          <div className="flex flex-wrap gap-2 pt-1">
            {statusEntries.map(([status, count]) => (
              <span
                key={status}
                className={`rounded-full px-2.5 py-0.5 text-xs font-semibold ${subscriptionStatusColor(status)}`}
              >
                {subscriptionStatusLabel(status)} · {count}
              </span>
            ))}
          </div>
        )}
      </div>

      <div className="flex flex-col gap-3 py-5">
        <SectionLabel>Platform Usage — Active Users This Month</SectionLabel>
        {totalActive === 0 ? (
          <p className="text-sm text-slate-400">No active sessions recorded this month.</p>
        ) : (
          <>
            <div className="flex h-2.5 overflow-hidden rounded-full bg-slate-100">
              <div className="bg-indigo-600" style={{ width: `${webPercent}%` }} />
              <div className="bg-pink-600" style={{ width: `${100 - webPercent}%` }} />
            </div>
            <div className="flex gap-6">
              <div className="flex items-center gap-2">
                <span className="inline-block h-2 w-2 rounded-full bg-indigo-600" />
                <span className="text-sm text-slate-700">
                  Web · <strong>{webCount}</strong> active user{webCount === 1 ? '' : 's'}
                </span>
              </div>
              <div className="flex items-center gap-2">
                <span className="inline-block h-2 w-2 rounded-full bg-pink-600" />
                <span className="text-sm text-slate-700">
                  Mobile · <strong>{mobileCount}</strong> active user{mobileCount === 1 ? '' : 's'}
                </span>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
