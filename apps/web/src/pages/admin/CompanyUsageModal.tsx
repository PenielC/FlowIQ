import { X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { fetchCompanyUsage, sinceText, type CompanyUsage } from '../../lib/usageApi'
import type { AdminCompanyRowResponse } from '../../lib/types'

/** One business's use of each part of FinFlow, for the admin (e.g. before calling the customer). */
export function CompanyUsageModal({ company, onClose }: { company: AdminCompanyRowResponse; onClose: () => void }) {
  const [usage, setUsage] = useState<CompanyUsage | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchCompanyUsage(company.id)
      .then(setUsage)
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load'))
  }, [company.id])

  const sections = usage
    ? (['Page', 'Action'] as const).map((kind) => ({
        kind,
        rows: usage.features
          .filter((f) => f.kind === kind)
          .sort((a, b) => (b.lastUsedUtc ?? '').localeCompare(a.lastUsedUtc ?? '') || a.label.localeCompare(b.label)),
      }))
    : []

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4">
      <div className="max-h-[90vh] w-full max-w-xl overflow-y-auto rounded-2xl bg-white p-6 shadow-xl" role="dialog" aria-label={`Usage for ${company.name}`}>
        <div className="mb-1 flex items-center justify-between">
          <h2 className="text-lg font-bold text-slate-900">{company.name}</h2>
          <button type="button" onClick={onClose} aria-label="Close" className="text-slate-400 hover:text-slate-600">
            <X size={20} />
          </button>
        </div>
        {error && <p className="text-sm text-red-600">{error}</p>}
        {!usage && !error && <p className="text-sm text-slate-400">Loading…</p>}
        {usage && (
          <>
            <p className="mb-4 text-sm text-slate-500" data-testid="company-usage-summary">
              Last active: <span className="font-medium text-slate-800">{sinceText(usage.lastActiveUtc)}</span> · {usage.activeUsers30} user
              {usage.activeUsers30 === 1 ? '' : 's'} active in the last 30 days
            </p>
            {sections.map((s) => (
              <table key={s.kind} className="mb-4 w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
                    <th className="py-2 font-medium">{s.kind === 'Page' ? 'Module' : 'Action'}</th>
                    <th className="py-2 font-medium">Last used</th>
                    <th className="py-2 text-right font-medium">Days used (30d)</th>
                    <th className="py-2 text-right font-medium">Uses (30d)</th>
                  </tr>
                </thead>
                <tbody>
                  {s.rows.map((f) => (
                    <tr key={f.key} className={`border-b border-slate-50 ${f.lastUsedUtc ? '' : 'text-slate-400'}`}>
                      <td className="py-1.5">{f.label}</td>
                      <td className="py-1.5">{sinceText(f.lastUsedUtc)}</td>
                      <td className="py-1.5 text-right tabular-nums">{f.daysActive30}</td>
                      <td className="py-1.5 text-right tabular-nums">{f.uses30}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ))}
          </>
        )}
      </div>
    </div>
  )
}
