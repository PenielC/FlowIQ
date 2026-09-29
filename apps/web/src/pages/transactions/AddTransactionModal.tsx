import { Sparkles, X } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { suggestCategory } from '../../lib/categorisationApi'
import { useAuth } from '../../lib/AuthContext'
import { currencies } from '../../lib/currencies'
import { fetchExchangeRate } from '../../lib/exchangeRatesApi'
import { RateAttribution } from '../../components/RateAttribution'
import { createTransaction } from '../../lib/transactionsApi'
import { transactionCategories } from '../../lib/types'

export function AddTransactionModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const { user } = useAuth()
  const companyCurrency = user?.companyCurrency ?? 'USD'
  const [description, setDescription] = useState('')
  const [category, setCategory] = useState<string>(transactionCategories[0])
  const [categoryTouched, setCategoryTouched] = useState(false)
  const [suggestedCategory, setSuggestedCategory] = useState<string | null>(null)
  const [amount, setAmount] = useState('')
  const [type, setType] = useState<'income' | 'expense'>('expense')
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [currency, setCurrency] = useState(companyCurrency)
  const [exchangeRate, setExchangeRate] = useState('')
  const [rateIsLive, setRateIsLive] = useState(true)
  const [rateSource, setRateSource] = useState<string | null>(null)
  const [isFetchingRate, setIsFetchingRate] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  function handleCurrencyChange(newCurrency: string) {
    setCurrency(newCurrency)
    if (newCurrency === companyCurrency) {
      setExchangeRate('')
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
        setExchangeRate('')
      })
      .finally(() => setIsFetchingRate(false))
  }

  useEffect(() => {
    if (description.trim().length < 3) return
    let cancelled = false
    const timer = setTimeout(() => {
      suggestCategory(description)
        .then((res) => {
          if (cancelled || res.confidence <= 0) return
          setSuggestedCategory(res.category)
          if (!categoryTouched) setCategory(res.category)
        })
        .catch(() => {
          // Best-effort suggestion — leave the manually-selected category untouched on failure.
        })
    }, 400)
    return () => {
      cancelled = true
      clearTimeout(timer)
    }
  }, [description, categoryTouched])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)

    const numericAmount = Number(amount)
    if (!numericAmount || numericAmount <= 0) {
      setError('Enter an amount greater than zero.')
      return
    }

    const numericRate = exchangeRate ? Number(exchangeRate) : undefined
    if (currency !== companyCurrency && exchangeRate && (!numericRate || numericRate <= 0)) {
      setError('Enter a valid exchange rate.')
      return
    }

    setIsSubmitting(true)
    try {
      await createTransaction({
        description,
        category,
        amount: type === 'income' ? numericAmount : -numericAmount,
        transactionDateUtc: new Date(date).toISOString(),
        status: 'Completed',
        currency,
        exchangeRate: currency === companyCurrency ? undefined : numericRate,
      })
      onCreated()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create transaction')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4">
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold text-slate-900">Add Transaction</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setType('income')}
              className={`flex-1 rounded-lg border px-3 py-2 text-sm font-medium ${
                type === 'income' ? 'border-emerald-500 bg-emerald-50 text-emerald-700' : 'border-slate-200 text-slate-500'
              }`}
            >
              Income
            </button>
            <button
              type="button"
              onClick={() => setType('expense')}
              className={`flex-1 rounded-lg border px-3 py-2 text-sm font-medium ${
                type === 'expense' ? 'border-red-500 bg-red-50 text-red-700' : 'border-slate-200 text-slate-500'
              }`}
            >
              Expense
            </button>
          </div>

          <div>
            <label htmlFor="description" className="mb-1 block text-sm font-medium text-slate-700">
              Description
            </label>
            <input
              id="description"
              required
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="category" className="mb-1 block text-sm font-medium text-slate-700">
                Category
              </label>
              <select
                id="category"
                value={category}
                onChange={(e) => {
                  setCategoryTouched(true)
                  setCategory(e.target.value)
                }}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
              >
                {transactionCategories.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
              {suggestedCategory && !categoryTouched && description.trim().length >= 3 && (
                <p className="mt-1 flex items-center gap-1 text-xs text-brand-purple">
                  <Sparkles size={12} />
                  Suggested from description
                </p>
              )}
            </div>
            <div>
              <label htmlFor="amount" className="mb-1 block text-sm font-medium text-slate-700">
                Amount
              </label>
              <input
                id="amount"
                type="number"
                step="0.01"
                min="0"
                required
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="currency" className="mb-1 block text-sm font-medium text-slate-700">
                Currency
              </label>
              <select
                id="currency"
                value={currency}
                onChange={(e) => handleCurrencyChange(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
              >
                {currencies.map((c) => (
                  <option key={c.code} value={c.code}>
                    {c.code} — {c.name}
                  </option>
                ))}
              </select>
            </div>
            {currency !== companyCurrency && (
              <div>
                <label htmlFor="exchangeRate" className="mb-1 block text-sm font-medium text-slate-700">
                  Exchange rate to {companyCurrency}
                </label>
                <input
                  id="exchangeRate"
                  type="number"
                  step="0.000001"
                  min="0"
                  required
                  disabled={isFetchingRate}
                  value={exchangeRate}
                  onChange={(e) => setExchangeRate(e.target.value)}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none disabled:opacity-60"
                />
                {isFetchingRate && <p className="mt-1 text-xs text-slate-400">Fetching live rate…</p>}
                {!isFetchingRate && !rateIsLive && (
                  <p className="mt-1 text-xs text-amber-600">Couldn't fetch a live rate — please enter it manually.</p>
                )}
                <RateAttribution source={rateSource} />
              </div>
            )}
          </div>

          <div>
            <label htmlFor="date" className="mb-1 block text-sm font-medium text-slate-700">
              Date
            </label>
            <input
              id="date"
              type="date"
              required
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
            />
          </div>

          {error && <p className="text-sm text-red-500">{error}</p>}

          <button
            type="submit"
            disabled={isSubmitting}
            className="mt-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
          >
            {isSubmitting ? 'Adding…' : 'Add Transaction'}
          </button>
        </form>
      </div>
    </div>
  )
}
