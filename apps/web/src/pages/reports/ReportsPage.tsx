import { Download } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { useAuth } from '../../lib/AuthContext'
import { categoryColor, categoryLabel, currencySymbol, formatCurrency } from '../../lib/categoryDisplay'
import { invoiceStatusColor, invoiceStatusLabel } from '../../lib/invoiceDisplay'
import { downloadTransactionsCsv, fetchReportsSummary } from '../../lib/reportsApi'
import type { ReportsSummaryResponse } from '../../lib/types'

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export function ReportsPage() {
  const { user } = useAuth()
  const currency = user?.companyCurrency ?? 'USD'
  const symbol = currencySymbol(currency)
  const [summary, setSummary] = useState<ReportsSummaryResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isExporting, setIsExporting] = useState(false)

  useEffect(() => {
    let cancelled = false
    fetchReportsSummary(6)
      .then((res) => !cancelled && setSummary(res))
      .catch(() => !cancelled && setSummary(null))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [])

  async function handleExport() {
    setIsExporting(true)
    try {
      await downloadTransactionsCsv()
    } catch {
      // Best-effort — the button simply stays clickable to retry.
    } finally {
      setIsExporting(false)
    }
  }

  const trendData = (summary?.monthlyTrend ?? []).map((p) => ({
    label: MONTH_LABELS[p.month - 1],
    Revenue: p.revenue,
    Expenses: p.expenses,
  }))

  const maxCategoryTotal = Math.max(1, ...(summary?.categoryBreakdown ?? []).map((c) => c.total))

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Reports</h1>
          <p className="text-sm text-slate-500">Trends and breakdowns over your last 6 months.</p>
        </div>
        <button
          type="button"
          onClick={handleExport}
          disabled={isExporting}
          className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
        >
          <Download size={16} />
          {isExporting ? 'Exporting…' : 'Export Transactions CSV'}
        </button>
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <h2 className="mb-4 text-base font-semibold text-slate-900">Revenue vs Expenses</h2>
        <div className="h-72">
          {isLoading ? (
            <div className="flex h-full items-center justify-center text-sm text-slate-400">Loading…</div>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={trendData} margin={{ left: -16, right: 8, top: 8 }}>
                <CartesianGrid vertical={false} stroke="#eef2f7" />
                <XAxis dataKey="label" tickLine={false} axisLine={false} tick={{ fill: '#94a3b8', fontSize: 12 }} />
                <YAxis
                  tickFormatter={(v: number) => `${symbol}${(v / 1000).toFixed(0)}k`}
                  tickLine={false}
                  axisLine={false}
                  tick={{ fill: '#94a3b8', fontSize: 12 }}
                />
                <Tooltip formatter={(value) => formatCurrency(Number(value), currency)} />
                <Legend />
                <Bar dataKey="Revenue" fill="#10b981" radius={[4, 4, 0, 0]} />
                <Bar dataKey="Expenses" fill="#f43f5e" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="mb-4 text-base font-semibold text-slate-900">Spending by Category</h2>
          {isLoading ? (
            <p className="text-sm text-slate-400">Loading…</p>
          ) : !summary || summary.categoryBreakdown.length === 0 ? (
            <p className="text-sm text-slate-400">No expenses in this period.</p>
          ) : (
            <ul className="flex flex-col gap-3">
              {summary.categoryBreakdown.map((c) => (
                <li key={c.category}>
                  <div className="mb-1 flex items-center justify-between text-sm">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${categoryColor(c.category)}`}>
                      {categoryLabel(c.category)}
                    </span>
                    <span className="font-semibold text-slate-900">{formatCurrency(c.total, currency)}</span>
                  </div>
                  <div className="h-2 w-full overflow-hidden rounded-full bg-slate-100">
                    <div
                      className="h-full rounded-full bg-brand-primary"
                      style={{ width: `${(c.total / maxCategoryTotal) * 100}%` }}
                    />
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="mb-4 text-base font-semibold text-slate-900">Invoices by Status</h2>
          {isLoading ? (
            <p className="text-sm text-slate-400">Loading…</p>
          ) : !summary || summary.invoiceStatusBreakdown.length === 0 ? (
            <p className="text-sm text-slate-400">No invoices yet.</p>
          ) : (
            <ul className="flex flex-col gap-3">
              {summary.invoiceStatusBreakdown.map((s) => (
                <li key={s.status} className="flex items-center justify-between rounded-lg border border-slate-100 px-3 py-2">
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${invoiceStatusColor(s.status)}`}>
                    {invoiceStatusLabel(s.status)}
                  </span>
                  <span className="text-xs text-slate-500">{s.count} invoice{s.count === 1 ? '' : 's'}</span>
                  <span className="font-semibold text-slate-900">{formatCurrency(s.totalAmount, currency)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  )
}
