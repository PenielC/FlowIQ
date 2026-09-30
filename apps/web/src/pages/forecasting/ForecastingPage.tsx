import { ArrowDownRight, ArrowUpRight } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../../lib/AuthContext'
import { formatCurrency } from '../../lib/categoryDisplay'
import { fetchForecast, fetchOwnerDraws } from '../../lib/forecastApi'
import type { CashFlowForecastResponse, OwnerDrawResponse } from '../../lib/types'
import { ForecastChart } from '../dashboard/ForecastChart'
import { OwnerDrawsPanel } from './OwnerDrawsPanel'

const RANGE_OPTIONS = [
  { label: '30 days', historyDays: 30, forecastDays: 30 },
  { label: '60 days', historyDays: 60, forecastDays: 60 },
  { label: '90 days', historyDays: 90, forecastDays: 90 },
] as const

function eventDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', timeZone: 'UTC' })
}

export function ForecastingPage() {
  const { user } = useAuth()
  const currency = user?.companyCurrency ?? 'USD'
  const [rangeIndex, setRangeIndex] = useState(0)
  const [forecast, setForecast] = useState<CashFlowForecastResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [draws, setDraws] = useState<OwnerDrawResponse[]>([])
  const [isDrawsLoading, setIsDrawsLoading] = useState(true)
  const [reloadKey, setReloadKey] = useState(0)

  const range = RANGE_OPTIONS[rangeIndex]

  useEffect(() => {
    let cancelled = false
    fetchForecast(range.historyDays, range.forecastDays)
      .then((res) => !cancelled && setForecast(res))
      .catch(() => !cancelled && setForecast(null))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [range.historyDays, range.forecastDays, reloadKey])

  const loadDraws = useCallback(() => {
    fetchOwnerDraws()
      .then((res) => setDraws(res.draws))
      .catch(() => setDraws([]))
      .finally(() => setIsDrawsLoading(false))
  }, [])

  useEffect(loadDraws, [loadDraws])

  function selectRange(i: number) {
    setIsLoading(true)
    setRangeIndex(i)
  }

  function handleDrawsChanged() {
    loadDraws()
    setIsLoading(true)
    setReloadKey((k) => k + 1)
  }

  const points = forecast?.points ?? []
  const current = forecast?.currentBalance ?? null
  const forecastedEnd = points.length > 0 ? points[points.length - 1].forecast : null
  const change = current !== null && forecastedEnd !== null ? forecastedEnd - current : null
  const lowest = forecast?.lowestBalance ?? null
  const events = forecast?.events ?? []

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Forecasting</h1>
          <p className="text-sm text-slate-500">
            Your everyday trend, plus planned withdrawals and pending invoices on the days they happen.
          </p>
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

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-xs text-slate-500">Current Balance</p>
          <p className="mt-1 text-xl font-bold text-slate-900">
            {isLoading || current === null ? '…' : formatCurrency(current, currency)}
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
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-xs text-slate-500">Lowest Point</p>
          <p className={`mt-1 text-xl font-bold ${lowest !== null && lowest < 0 ? 'text-red-600' : 'text-slate-900'}`}>
            {isLoading || lowest === null ? '…' : formatCurrency(lowest, currency)}
          </p>
          {!isLoading && forecast?.lowestBalanceDateUtc && (
            <p className="text-xs text-slate-500">around {eventDate(forecast.lowestBalanceDateUtc)}</p>
          )}
        </div>
      </div>

      <ForecastChart forecast={forecast} isLoading={isLoading} forecastDays={range.forecastDays} currency={currency} />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="text-base font-semibold text-slate-900">Planned personal withdrawals</h2>
          <p className="mb-4 text-xs text-slate-500">
            School fees, home rent and other money you take out for yourself. Each one is placed on its own date.
          </p>
          <OwnerDrawsPanel draws={draws} isLoading={isDrawsLoading} onChanged={handleDrawsChanged} />
        </section>

        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="text-base font-semibold text-slate-900">Coming up in the next {range.forecastDays} days</h2>
          <p className="mb-4 text-xs text-slate-500">Known amounts in the forecast, on the days they're due.</p>
          {isLoading ? (
            <p className="text-sm text-slate-400">Loading…</p>
          ) : events.length === 0 ? (
            <p className="text-sm text-slate-500">Nothing scheduled. Add planned withdrawals or send invoices to see them here.</p>
          ) : (
            <ul className="divide-y divide-slate-100">
              {events.map((e, i) => (
                <li key={i} className="flex items-center justify-between gap-3 py-2.5">
                  <div className="flex min-w-0 items-center gap-3">
                    <span
                      className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-lg ${
                        e.amount < 0 ? 'bg-amber-100 text-amber-700' : 'bg-emerald-100 text-emerald-700'
                      }`}
                    >
                      {e.amount < 0 ? <ArrowDownRight size={16} /> : <ArrowUpRight size={16} />}
                    </span>
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium text-slate-900">
                        {e.kind === 'InvoiceDue' ? `Invoice due: ${e.label}` : e.label}
                      </p>
                      <p className="text-xs text-slate-500">{eventDate(e.dateUtc)}</p>
                    </div>
                  </div>
                  <p className={`shrink-0 text-sm font-semibold ${e.amount < 0 ? 'text-amber-700' : 'text-emerald-700'}`}>
                    {e.amount < 0 ? '-' : '+'}
                    {formatCurrency(Math.abs(e.amount), currency)}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  )
}
