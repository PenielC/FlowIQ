import { useEffect, useState } from 'react'
import { useAuth } from '../../lib/AuthContext'
import { formatCurrency } from '../../lib/categoryDisplay'
import { fetchForecast } from '../../lib/forecastApi'
import type { CashFlowPointResponse } from '../../lib/types'
import { ForecastChart } from '../dashboard/ForecastChart'

const RANGE_OPTIONS = [
  { label: '30 days', historyDays: 30, forecastDays: 30 },
  { label: '60 days', historyDays: 60, forecastDays: 60 },
  { label: '90 days', historyDays: 90, forecastDays: 90 },
] as const

export function ForecastingPage() {
  const { user } = useAuth()
  const currency = user?.companyCurrency ?? 'USD'
  const [rangeIndex, setRangeIndex] = useState(0)
  const [points, setPoints] = useState<CashFlowPointResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)

  const range = RANGE_OPTIONS[rangeIndex]

  useEffect(() => {
    let cancelled = false
    fetchForecast(range.historyDays, range.forecastDays)
      .then((res) => !cancelled && setPoints(res.points))
      .catch(() => !cancelled && setPoints([]))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [range.historyDays, range.forecastDays])

  function selectRange(i: number) {
    setIsLoading(true)
    setRangeIndex(i)
  }

  const lastActual = [...points].reverse().find((p) => p.actual !== null)?.actual ?? null
  const forecastedEnd = points.length > 0 ? points[points.length - 1].forecast : null
  const change = lastActual !== null && forecastedEnd !== null ? forecastedEnd - lastActual : null

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Forecasting</h1>
          <p className="text-sm text-slate-500">Trend-based projection of your cash position.</p>
        </div>
        <div className="flex gap-1 rounded-lg border border-slate-200 bg-white p-1">
          {RANGE_OPTIONS.map((option, i) => (
            <button
              key={option.label}
              type="button"
              onClick={() => selectRange(i)}
              className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                i === rangeIndex ? 'bg-brand-primary text-white' : 'text-slate-500 hover:bg-slate-100'
              }`}
            >
              {option.label}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-xs text-slate-500">Current Balance</p>
          <p className="mt-1 text-xl font-bold text-slate-900">
            {isLoading || lastActual === null ? '…' : formatCurrency(lastActual, currency)}
          </p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-xs text-slate-500">Projected in {range.forecastDays} days</p>
          <p className="mt-1 text-xl font-bold text-slate-900">
            {isLoading || forecastedEnd === null ? '…' : formatCurrency(forecastedEnd, currency)}
          </p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-xs text-slate-500">Projected Change</p>
          <p className={`mt-1 text-xl font-bold ${change !== null && change < 0 ? 'text-red-600' : 'text-emerald-600'}`}>
            {isLoading || change === null ? '…' : `${change >= 0 ? '+' : '-'}${formatCurrency(Math.abs(change), currency)}`}
          </p>
        </div>
      </div>

      <ForecastChart points={points} isLoading={isLoading} forecastDays={range.forecastDays} currency={currency} />
    </div>
  )
}
