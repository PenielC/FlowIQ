import { Sparkles } from 'lucide-react'
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { currencySymbol, formatCurrency } from '../../lib/categoryDisplay'
import type { CashFlowPointResponse } from '../../lib/types'

function abbreviatedAmount(value: number, symbol: string) {
  const sign = value < 0 ? '-' : ''
  return `${sign}${symbol}${(Math.abs(value) / 1000).toFixed(0)}k`
}

function shortDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

function ChartTooltip({
  active,
  payload,
  label,
  currency,
}: {
  active?: boolean
  payload?: { value: number }[]
  label?: string
  currency: string
}) {
  if (!active || !payload?.length) return null
  const value = payload[0]?.value
  return (
    <div className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs shadow-md">
      <p className="font-medium text-slate-500">{label}</p>
      <p className="text-sm font-bold text-slate-900">{formatCurrency(value, currency)}</p>
    </div>
  )
}

export function ForecastChart({
  points,
  isLoading,
  forecastDays = 30,
  currency,
}: {
  points: CashFlowPointResponse[]
  isLoading: boolean
  forecastDays?: number
  currency: string
}) {
  const chartData = points.map((p) => ({ date: shortDate(p.dateUtc), actual: p.actual, forecast: p.forecast }))
  const symbol = currencySymbol(currency)

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="mb-4 flex items-start justify-between">
        <div>
          <h2 className="flex items-center gap-2 text-base font-semibold text-slate-900">
            <Sparkles size={16} className="text-brand-purple" />
            Cash Flow Forecast
          </h2>
          <p className="text-xs text-slate-500">AI-powered {forecastDays}-day forecast</p>
        </div>
        <div className="flex items-center gap-4 text-xs text-slate-500">
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-brand-primary" /> Actual
          </span>
          <span className="flex items-center gap-1.5">
            <span className="h-0.5 w-3 border-t-2 border-dashed border-brand-purple" /> Forecast
          </span>
        </div>
      </div>

      <div className="h-64">
        {isLoading ? (
          <div className="flex h-full items-center justify-center text-sm text-slate-400">Loading…</div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={chartData} margin={{ left: -16, right: 8, top: 8 }}>
              <defs>
                <linearGradient id="actualFill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#10b981" stopOpacity={0.25} />
                  <stop offset="100%" stopColor="#10b981" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid vertical={false} stroke="#eef2f7" />
              <XAxis
                dataKey="date"
                tickLine={false}
                axisLine={false}
                tick={{ fill: '#94a3b8', fontSize: 12 }}
                interval="preserveStartEnd"
              />
              <YAxis
                tickFormatter={(value: number) => abbreviatedAmount(value, symbol)}
                tickLine={false}
                axisLine={false}
                tick={{ fill: '#94a3b8', fontSize: 12 }}
              />
              <Tooltip content={<ChartTooltip currency={currency} />} />
              <Area
                type="monotone"
                dataKey="actual"
                stroke="#10b981"
                strokeWidth={2.5}
                fill="url(#actualFill)"
                connectNulls
                dot={false}
              />
              <Area
                type="monotone"
                dataKey="forecast"
                stroke="#7c3aed"
                strokeWidth={2.5}
                strokeDasharray="6 5"
                fill="transparent"
                connectNulls
                dot={false}
              />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  )
}
