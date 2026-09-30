import { X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { RateAttribution } from '../../components/RateAttribution'
import { useAuth } from '../../lib/AuthContext'
import { currencies } from '../../lib/currencies'
import { fetchExchangeRate } from '../../lib/exchangeRatesApi'
import { createOwnerDraw, updateOwnerDraw } from '../../lib/forecastApi'
import { drawFrequencies, type DrawFrequency, type OwnerDrawResponse } from '../../lib/types'
import { FREQUENCY_LABELS, MONTH_NAMES, firstOfNextMonth } from './ownerDrawDisplay'

const inputClass =
  'w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none disabled:opacity-60'

/** Add or edit a planned personal withdrawal (school fees, home rent...). */
export function OwnerDrawModal({
  draw,
  preset,
  onClose,
  onSaved,
}: {
  draw?: OwnerDrawResponse
  preset?: { name: string; frequency: DrawFrequency; months?: number[] }
  onClose: () => void
  onSaved: () => void
}) {
  const { user } = useAuth()
  const companyCurrency = user?.companyCurrency ?? 'USD'
  const [name, setName] = useState(draw?.name ?? preset?.name ?? '')
  const [amount, setAmount] = useState(draw ? String(draw.amount) : '')
  const [currency, setCurrency] = useState(draw?.currency ?? companyCurrency)
  const [exchangeRate, setExchangeRate] = useState('')
  const [rateSource, setRateSource] = useState<string | null>(null)
  const [rateIsLive, setRateIsLive] = useState(true)
  const [isFetchingRate, setIsFetchingRate] = useState(false)
  const [frequency, setFrequency] = useState<DrawFrequency>(draw?.frequency ?? preset?.frequency ?? 'Monthly')
  const [nextDate, setNextDate] = useState(draw ? draw.nextDateUtc.slice(0, 10) : firstOfNextMonth())
  const [months, setMonths] = useState<number[]>(draw?.months.length ? draw.months : (preset?.months ?? []))
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  function handleCurrencyChange(newCurrency: string) {
    setCurrency(newCurrency)
    setExchangeRate('')
    if (newCurrency === companyCurrency) {
      setRateIsLive(true)
      return
    }
    setIsFetchingRate(true)
    fetchExchangeRate(newCurrency, companyCurrency)
      .then((res) => {
        setRateIsLive(res.isLive)
        setRateSource(res.source)
        setExchangeRate(res.rate !== null ? String(res.rate) : '')
      })
      .catch(() => {
        setRateIsLive(false)
        setRateSource(null)
      })
      .finally(() => setIsFetchingRate(false))
  }

  function toggleMonth(m: number) {
    setMonths((current) => (current.includes(m) ? current.filter((x) => x !== m) : [...current, m].sort((a, b) => a - b)))
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    const numericAmount = Number(amount)
    if (!numericAmount || numericAmount <= 0) {
      setError('Enter an amount greater than zero.')
      return
    }
    if (frequency === 'SelectedMonths' && months.length === 0) {
      setError('Choose at least one month.')
      return
    }
    const numericRate = exchangeRate ? Number(exchangeRate) : undefined
    if (currency !== companyCurrency && exchangeRate && (!numericRate || numericRate <= 0)) {
      setError('Enter a valid exchange rate.')
      return
    }

    const request = {
      name: name.trim(),
      amount: numericAmount,
      currency,
      exchangeRate: currency === companyCurrency ? undefined : numericRate,
      frequency,
      nextDateUtc: nextDate,
      months: frequency === 'SelectedMonths' ? months : [],
    }
    setIsSubmitting(true)
    try {
      if (draw) await updateOwnerDraw(draw.id, request)
      else await createOwnerDraw(request)
      onSaved()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save the withdrawal')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4">
      <div className="max-h-[90vh] w-full max-w-md overflow-y-auto rounded-2xl bg-white p-6 shadow-xl">
        <div className="mb-1 flex items-center justify-between">
          <h2 className="text-lg font-bold text-slate-900">{draw ? 'Edit planned withdrawal' : 'Add a planned withdrawal'}</h2>
          <button type="button" onClick={onClose} aria-label="Close" className="text-slate-400 hover:text-slate-600">
            <X size={20} />
          </button>
        </div>
        <p className="mb-4 text-sm text-slate-500">Money you regularly take out of the business for personal costs.</p>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div>
            <label htmlFor="drawName" className="mb-1 block text-sm font-medium text-slate-700">
              What is it for?
            </label>
            <input
              id="drawName"
              required
              maxLength={100}
              placeholder="e.g. School fees"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className={inputClass}
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="drawAmount" className="mb-1 block text-sm font-medium text-slate-700">
                Amount each time
              </label>
              <input
                id="drawAmount"
                type="number"
                step="0.01"
                min="0"
                required
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="drawCurrency" className="mb-1 block text-sm font-medium text-slate-700">
                Currency
              </label>
              <select id="drawCurrency" value={currency} onChange={(e) => handleCurrencyChange(e.target.value)} className={inputClass}>
                {currencies.map((c) => (
                  <option key={c.code} value={c.code}>
                    {c.code} — {c.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {currency !== companyCurrency && (
            <div>
              <label htmlFor="drawRate" className="mb-1 block text-sm font-medium text-slate-700">
                Exchange rate to {companyCurrency}
              </label>
              <input
                id="drawRate"
                type="number"
                step="0.000001"
                min="0"
                required
                disabled={isFetchingRate}
                value={exchangeRate}
                onChange={(e) => setExchangeRate(e.target.value)}
                className={inputClass}
              />
              {isFetchingRate && <p className="mt-1 text-xs text-slate-400">Fetching live rate…</p>}
              {!isFetchingRate && !rateIsLive && (
                <p className="mt-1 text-xs text-amber-600">Couldn't fetch a live rate — please enter it manually.</p>
              )}
              <RateAttribution source={rateSource} />
            </div>
          )}

          <div>
            <label htmlFor="drawFrequency" className="mb-1 block text-sm font-medium text-slate-700">
              How often?
            </label>
            <select
              id="drawFrequency"
              value={frequency}
              onChange={(e) => setFrequency(e.target.value as DrawFrequency)}
              className={inputClass}
            >
              {drawFrequencies.map((f) => (
                <option key={f} value={f}>
                  {FREQUENCY_LABELS[f]}
                </option>
              ))}
            </select>
          </div>

          {frequency === 'SelectedMonths' && (
            <fieldset>
              <legend className="mb-1 block text-sm font-medium text-slate-700">Which months?</legend>
              <p className="mb-2 text-xs text-slate-500">For school fees, pick the months each term's fees are due.</p>
              <div className="grid grid-cols-6 gap-1.5">
                {MONTH_NAMES.map((label, i) => {
                  const m = i + 1
                  const on = months.includes(m)
                  return (
                    <button
                      key={label}
                      type="button"
                      aria-pressed={on}
                      onClick={() => toggleMonth(m)}
                      className={`rounded-md border px-1 py-1.5 text-xs font-medium ${
                        on ? 'border-brand-primary bg-emerald-50 text-emerald-700' : 'border-slate-200 text-slate-500 hover:bg-slate-50'
                      }`}
                    >
                      {label}
                    </button>
                  )
                })}
              </div>
            </fieldset>
          )}

          <div>
            <label htmlFor="drawNextDate" className="mb-1 block text-sm font-medium text-slate-700">
              {frequency === 'Once' ? 'Date' : 'Next payment date'}
            </label>
            <input
              id="drawNextDate"
              type="date"
              required
              value={nextDate}
              onChange={(e) => setNextDate(e.target.value)}
              className={inputClass}
            />
            {frequency !== 'Once' && (
              <p className="mt-1 text-xs text-slate-500">
                {frequency === 'Weekly' ? 'It repeats on this day of the week.' : 'It repeats on this day of the month.'}
              </p>
            )}
          </div>

          {error && <p className="text-sm text-red-500">{error}</p>}

          <button
            type="submit"
            disabled={isSubmitting}
            className="mt-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
          >
            {isSubmitting ? 'Saving…' : draw ? 'Save changes' : 'Add withdrawal'}
          </button>
        </form>
      </div>
    </div>
  )
}
