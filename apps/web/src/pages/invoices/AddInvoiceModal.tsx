import { X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { CustomerPicker } from '../../components/CustomerPicker'
import { useAuth } from '../../lib/AuthContext'
import { currencies } from '../../lib/currencies'
import { fetchExchangeRate } from '../../lib/exchangeRatesApi'
import { createInvoice } from '../../lib/invoicesApi'

export function AddInvoiceModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const { user } = useAuth()
  const companyCurrency = user?.companyCurrency ?? 'USD'
  const [customerName, setCustomerName] = useState('')
  const [amount, setAmount] = useState('')
  const [issueDate, setIssueDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [dueDate, setDueDate] = useState(() => new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10))
  const [currency, setCurrency] = useState(companyCurrency)
  const [exchangeRate, setExchangeRate] = useState('')
  const [rateIsLive, setRateIsLive] = useState(true)
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
        setExchangeRate(res.rate !== null ? String(res.rate) : '')
      })
      .catch(() => {
        setRateIsLive(false)
        setExchangeRate('')
      })
      .finally(() => setIsFetchingRate(false))
  }

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
      await createInvoice({
        customerName,
        amount: numericAmount,
        issueDateUtc: new Date(issueDate).toISOString(),
        dueDateUtc: new Date(dueDate).toISOString(),
        currency,
        exchangeRate: currency === companyCurrency ? undefined : numericRate,
      })
      onCreated()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create invoice')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4">
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold text-slate-900">New Invoice</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div>
            <label htmlFor="customerName" className="mb-1 block text-sm font-medium text-slate-700">
              Customer
            </label>
            <CustomerPicker id="customerName" value={customerName} onChange={setCustomerName} />
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
              </div>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="issueDate" className="mb-1 block text-sm font-medium text-slate-700">
                Issue Date
              </label>
              <input
                id="issueDate"
                type="date"
                required
                value={issueDate}
                onChange={(e) => setIssueDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
              />
            </div>
            <div>
              <label htmlFor="dueDate" className="mb-1 block text-sm font-medium text-slate-700">
                Due Date
              </label>
              <input
                id="dueDate"
                type="date"
                required
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
              />
            </div>
          </div>

          {error && <p className="text-sm text-red-500">{error}</p>}

          <button
            type="submit"
            disabled={isSubmitting}
            className="mt-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
          >
            {isSubmitting ? 'Creating…' : 'Create Invoice'}
          </button>
        </form>
      </div>
    </div>
  )
}
