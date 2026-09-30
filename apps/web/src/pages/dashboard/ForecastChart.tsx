import { Sparkles } from 'lucide-react'
import {
  Area,
  AreaChart,
  CartesianGrid,
  ReferenceDot,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { currencySymbol, formatCurrency } from '../../lib/categoryDisplay'
import type { CashFlowForecastResponse, ForecastEventResponse } from '../../lib/types'

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
  eventsByDate,
}: {
  active?: boolean
  payload?: { value: number }[]
  label?: string
  currency: string
  eventsByDate: Map<string, ForecastEventResponse[]>
}) {
  if (!active || !payload?.length) return null
  const value = payload[0]?.value
  const events = (label && eventsByDate.get(label)) || []
  return (
    <div className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs shadow-md">
      <p className="font-medium text-slate-500">{label}</p>
      <p className="text-sm font-bold text-slate-900">{formatCurrency(value, currency)}</p>
      {events.map((e, i) => (
        <p key={i} className={e.amount < 0 ? 'text-amber-700' : 'text-emerald-700'}>
          {e.kind === 'InvoiceDue' ? `Invoice due: ${e.label}` : e.label} ({e.amount < 0 ? '-' : '+'}
          {formatCurrency(Math.abs(e.amount), currency)})
        </p>
      ))}
    </div>
  )
}

export function ForecastChart({
  forecast,
  isLoading,
  forecastDays = 30,
  currency,
}: {
  forecast: CashFlowForecastResponse | null
  isLoading: boolean
  forecastDays?: number
  currency: string
}) {
  const points = forecast?.points ?? []
  const chartData = points.map((p) => ({ date: shortDate(p.dateUtc), actual: p.actual, forecast: p.forecast }))
  const symbol = currencySymbol(currency)

  const eventsByDate = new Map<string, ForecastEventResponse[]>()
  for (const e of forecast?.events ?? []) {
    const key = shortDate(e.dateUtc)
    eventsByDate.set(key, [...(eventsByDate.get(key) ?? []), e])
  }

  // Only mark the lowest point when it is a real dip below today's balance.
  const dip =
    forecast && forecast.lowestBalance !== null && forecast.lowestBalanceDateUtc && forecast.lowestBalance < forecast.currentBalance
      ? { date: shortDate(forecast.lowestBalanceDateUtc), value: forecast.lowestBalance, belowZero: forecast.lowestBalance < 0 }
      : null

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
              <Tooltip content={<ChartTooltip currency={currency} eventsByDate={eventsByDate} />} />
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
              {dip && (
                <ReferenceDot
                  x={dip.date}
                  y={dip.value}
                  r={6}
                  fill={dip.belowZero ? '#dc2626' : '#f59e0b'}
                  stroke="#fff"
                  strokeWidth={2}
                />
              )}
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>
      {!isLoading && dip && (
        <p className={`mt-3 flex items-center gap-2 text-xs ${dip.belowZero ? 'text-red-600' : 'text-amber-700'}`}>
          <span className={`h-2.5 w-2.5 rounded-full ${dip.belowZero ? 'bg-red-600' : 'bg-amber-500'}`} />
          Lowest point: {formatCurrency(dip.value, currency)} around {dip.date}
          {dip.belowZero ? ', you could run short' : ''}
        </p>
      )}
    </div>
  )
}
