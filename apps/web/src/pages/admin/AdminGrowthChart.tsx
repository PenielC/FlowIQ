import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { AdminMonthlyTrendPointResponse } from '../../lib/types'

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export function AdminGrowthChart({ trend, isLoading }: { trend: AdminMonthlyTrendPointResponse[]; isLoading: boolean }) {
  const chartData = trend.map((p) => ({
    label: `${MONTH_LABELS[p.month - 1]} ${p.year}`,
    'New Companies': p.newCompanies,
    'New Subscriptions': p.newSubscriptions,
  }))

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <h2 className="mb-4 text-base font-semibold text-slate-900">Growth — Last 6 Months</h2>
      <div className="h-72">
        {isLoading ? (
          <div className="flex h-full items-center justify-center text-sm text-slate-400">Loading…</div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData} margin={{ left: -16, right: 8, top: 8 }}>
              <CartesianGrid vertical={false} stroke="#eef2f7" />
              <XAxis dataKey="label" tickLine={false} axisLine={false} tick={{ fill: '#94a3b8', fontSize: 12 }} />
              <YAxis allowDecimals={false} tickLine={false} axisLine={false} tick={{ fill: '#94a3b8', fontSize: 12 }} />
              <Tooltip />
              <Legend />
              <Bar dataKey="New Companies" fill="#7c3aed" radius={[4, 4, 0, 0]} />
              <Bar dataKey="New Subscriptions" fill="#10b981" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  )
}
