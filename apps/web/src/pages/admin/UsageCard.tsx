import { Activity } from 'lucide-react'
import { useEffect, useState } from 'react'
import { fetchUsageOverview, type FeatureUsage, type UsageOverview } from '../../lib/usageApi'

const BRAND = '#10b981'

function weekLabel(iso: string) {
  return new Date(`${iso}T00:00:00Z`).toLocaleDateString('en-US', { month: 'short', day: 'numeric', timeZone: 'UTC' })
}

/**
 * Twelve weekly bars of businesses using a feature. One series, one hue; each bar shows its week and value on
 * hover (and to screen readers), and the same numbers' 7- and 30-day totals sit in the table beside it.
 */
function Trend({ feature, weekStarts }: { feature: FeatureUsage; weekStarts: string[] }) {
  const [hover, setHover] = useState<number | null>(null)
  const values = feature.weeklyCompanies
  const max = Math.max(1, ...values)
  const w = 7
  const gap = 2
  const h = 28
  return (
    <div className="relative flex items-center gap-2">
      <svg width={values.length * (w + gap)} height={h} role="img" aria-label={`${feature.label}: businesses per week, last 12 weeks`}>
        <line x1={0} x2={values.length * (w + gap)} y1={h - 0.5} y2={h - 0.5} stroke="#e2e8f0" />
        {values.map((v, i) => {
          const bh = v === 0 ? 0 : Math.max(3, (v / max) * (h - 2))
          return (
            <g key={i} onMouseEnter={() => setHover(i)} onMouseLeave={() => setHover(null)}>
              {/* Full-height hit area, wider than the bar. */}
              <rect x={i * (w + gap)} y={0} width={w + gap} height={h} fill="transparent" />
              {bh > 0 && (
                <rect x={i * (w + gap)} y={h - bh} width={w} height={bh} rx={2} fill={BRAND} opacity={hover === null || hover === i ? 1 : 0.45}>
                  <title>{`Week of ${weekLabel(weekStarts[i])}: ${v} business${v === 1 ? '' : 'es'}`}</title>
                </rect>
              )}
            </g>
          )
        })}
      </svg>
      <span className="w-28 text-[11px] text-slate-500">
        {hover === null ? '' : `${weekLabel(weekStarts[hover])}: ${values[hover]}`}
      </span>
    </div>
  )
}

function Table({ title, rows, overview }: { title: string; rows: FeatureUsage[]; overview: UsageOverview }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
            <th className="px-5 py-2 font-medium">{title}</th>
            <th className="px-3 py-2 text-right font-medium">Businesses 7d</th>
            <th className="px-3 py-2 text-right font-medium">Businesses 30d</th>
            <th className="px-3 py-2 text-right font-medium">Users 30d</th>
            <th className="px-3 py-2 text-right font-medium">Uses 30d</th>
            <th className="px-5 py-2 font-medium">Businesses per week (12 weeks)</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((f) => (
            <tr key={f.key} className="border-b border-slate-50" data-testid="usage-row">
              <td className="px-5 py-2 text-slate-800">{f.label}</td>
              <td className="px-3 py-2 text-right tabular-nums text-slate-700">{f.companies7}</td>
              <td className="px-3 py-2 text-right tabular-nums text-slate-700">
                {f.companies30}
                {overview.activeCompanies30 > 0 && (
                  <span className="ml-1 text-xs text-slate-400">({Math.round((f.companies30 / overview.activeCompanies30) * 100)}%)</span>
                )}
              </td>
              <td className="px-3 py-2 text-right tabular-nums text-slate-700">{f.users30}</td>
              <td className="px-3 py-2 text-right tabular-nums text-slate-700">{f.uses30}</td>
              <td className="px-5 py-2">
                <Trend feature={f} weekStarts={overview.weekStarts} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/** Platform admin: which parts of FinFlow businesses actually use. */
export function UsageCard() {
  const [overview, setOverview] = useState<UsageOverview | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchUsageOverview()
      .then(setOverview)
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load usage'))
  }, [])

  const byAdoption = (a: FeatureUsage, b: FeatureUsage) => b.companies30 - a.companies30 || a.label.localeCompare(b.label)
  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-sm" data-testid="usage-card">
      <div className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-100 p-5">
        <div>
          <h2 className="flex items-center gap-2 text-base font-semibold text-slate-900">
            <Activity size={17} className="text-brand-primary" /> Module usage
          </h2>
          <p className="mt-1 text-sm text-slate-500">Which parts of FinFlow businesses use. Page visits and key actions, counted per day; no content is recorded.</p>
        </div>
        {overview && (
          <div className="flex gap-6 text-right">
            <div>
              <p className="text-xs text-slate-500">Active businesses, 7 days</p>
              <p className="text-xl font-bold text-slate-900">{overview.activeCompanies7}</p>
            </div>
            <div>
              <p className="text-xs text-slate-500">Active businesses, 30 days</p>
              <p className="text-xl font-bold text-slate-900">{overview.activeCompanies30}</p>
            </div>
          </div>
        )}
      </div>
      {error && <p className="p-5 text-sm text-red-600">{error}</p>}
      {!overview && !error && <p className="p-5 text-sm text-slate-400">Loading…</p>}
      {overview && (
        <>
          <Table title="Page" rows={overview.features.filter((f) => f.kind === 'Page').sort(byAdoption)} overview={overview} />
          <div className="h-3" />
          <Table title="Action" rows={overview.features.filter((f) => f.kind === 'Action').sort(byAdoption)} overview={overview} />
          <div className="border-t border-slate-100 p-5">
            <h3 className="text-sm font-semibold text-slate-900">Feature take-up</h3>
            <p className="mb-3 text-xs text-slate-500">Businesses that have ever used each feature, from FinFlow&apos;s own records.</p>
            <ul className="grid grid-cols-1 gap-x-8 gap-y-1.5 sm:grid-cols-2" data-testid="feature-switches">
              {overview.switches.map((s) => (
                <li key={s.label} className="flex justify-between gap-4 text-sm">
                  <span className="text-slate-700">{s.label}</span>
                  <span className="tabular-nums font-semibold text-slate-900">{s.companies}</span>
                </li>
              ))}
            </ul>
          </div>
        </>
      )}
    </div>
  )
}
