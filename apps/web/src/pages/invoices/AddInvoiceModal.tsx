import { Plus, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { CustomerPicker } from '../../components/CustomerPicker'
import { useAuth } from '../../lib/AuthContext'
import { formatCurrency } from '../../lib/categoryDisplay'
import { currencies } from '../../lib/currencies'
import { fetchExchangeRate } from '../../lib/exchangeRatesApi'
import { RateAttribution } from '../../components/RateAttribution'
import { createInvoice } from '../../lib/invoicesApi'

interface LineItemRow {
  description: string
  amount: string
}

export function AddInvoiceModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const { user } = useAuth()
  const companyCurrency = user?.companyCurrency ?? 'USD'
  const [customerName, setCustomerName] = useState('')
  const [customerEmail, setCustomerEmail] = useState('')
  const [lineItems, setLineItems] = useState<LineItemRow[]>([{ description: '', amount: '' }])
  const [notes, setNotes] = useState('')
  const [issueDate, setIssueDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [dueDate, setDueDate] = useState(() => new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 10))
  const [currency, setCurrency] = useState(companyCurrency)
  const [exchangeRate, setExchangeRate] = useState('')
  const [rateIsLive, setRateIsLive] = useState(true)
  const [rateSource, setRateSource] = useState<string | null>(null)
  const [isFetchingRate, setIsFetchingRate] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const total = lineItems.reduce((sum, li) => sum + (Number(li.amount) || 0), 0)

  function updateLineItem(index: number, patch: Partial<LineItemRow>) {
    setLineItems((rows) => rows.map((row, i) => (i === index ? { ...row, ...patch } : row)))
  }

  function addLineItem() {
    setLineItems((rows) => [...rows, { description: '', amount: '' }])
  }

  function removeLineItem(index: number) {
    setLineItems((rows) => rows.filter((_, i) => i !== index))
  }

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

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)

    const cleanedLineItems = lineItems
      .map((li) => ({ description: li.description.trim(), amount: Number(li.amount) }))
      .filter((li) => li.description.length > 0 || li.amount > 0)

    if (cleanedLineItems.length === 0) {
      setError('Add at least one line item.')
      return
    }
    const invalidRow = cleanedLineItems.find((li) => !li.description || !li.amount || li.amount <= 0)
    if (invalidRow) {
      setError('Every line item needs a description and an amount greater than zero.')
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
        lineItems: cleanedLineItems,
        issueDateUtc: new Date(issueDate).toISOString(),
        dueDateUtc: new Date(dueDate).toISOString(),
        currency,
        exchangeRate: currency === companyCurrency ? undefined : numericRate,
        notes: notes.trim() || undefined,
        customerEmail: customerEmail.trim() || undefined,
      })
      onCreated()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create invoice')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 px-4 py-8">
      <div className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl">
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
            <CustomerPicker
              id="customerName"
              value={customerName}
              onChange={setCustomerName}
              onPick={(c) => c.email && setCustomerEmail(c.email)}
            />
          </div>

          <div>
            <label htmlFor="customerEmail" className="mb-1 block text-sm font-medium text-slate-700">
              Customer email <span className="font-normal text-slate-400">(optional, for emailing it and reminders)</span>
            </label>
            <input
              id="customerEmail"
              type="email"
              value={customerEmail}
              onChange={(e) => setCustomerEmail(e.target.value)}
              placeholder="accounts@customer.com"
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
            />
          </div>

          <div>
            <p className="mb-1 text-sm font-medium text-slate-700">Line Items</p>
            <div className="flex flex-col gap-2">
              {lineItems.map((row, i) => (
                <div key={i} className="flex items-center gap-2">
                  <input
                    aria-label={`Line item ${i + 1} description`}
                    placeholder="Description"
                    value={row.description}
                    onChange={(e) => updateLineItem(i, { description: e.target.value })}
                    className="flex-1 rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
                  />
                  <input
                    aria-label={`Line item ${i + 1} amount`}
                    type="number"
                    step="0.01"
                    min="0"
                    placeholder="0.00"
                    value={row.amount}
                    onChange={(e) => updateLineItem(i, { amount: e.target.value })}
                    className="w-28 rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
                  />
                  <button
                    type="button"
                    onClick={() => removeLineItem(i)}
                    disabled={lineItems.length === 1}
                    className="shrink-0 text-slate-300 hover:text-red-500 disabled:pointer-events-none disabled:opacity-0"
                  >
                    <X size={16} />
                  </button>
                </div>
              ))}
            </div>
            <button
              type="button"
              onClick={addLineItem}
              className="mt-2 flex items-center gap-1 text-xs font-medium text-brand-primary hover:underline"
            >
              <Plus size={14} />
              Add line item
            </button>
            <p className="mt-2 text-right text-sm font-semibold text-slate-700">
              Total: {formatCurrency(total, currency)}
            </p>
          </div>

          <div>
            <label htmlFor="notes" className="mb-1 block text-sm font-medium text-slate-700">
              Notes / Terms
            </label>
            <textarea
              id="notes"
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Optional — payment terms, thank-you note, etc."
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
                <RateAttribution source={rateSource} />
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
