import { X } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { previewCurrencyChange, updateCompanyCurrency } from '../../lib/companiesApi'
import type { CurrencyChangePreviewResponse } from '../../lib/types'

interface CurrencyChangeModalProps {
  fromCurrency: string
  toCurrency: string
  onClose: () => void
  onChanged: () => Promise<void> | void
}

function records(transactions: number, invoices: number) {
  const parts = []
  if (transactions) parts.push(`${transactions} transaction${transactions === 1 ? '' : 's'}`)
  if (invoices) parts.push(`${invoices} invoice${invoices === 1 ? '' : 's'}`)
  return parts.join(' and ')
}

/**
 * Confirms a reporting-currency change and shows what it converts. Every record is converted from its own
 * currency on the server; currencies without a live rate need one entered here first.
 */
export function CurrencyChangeModal({ fromCurrency, toCurrency, onClose, onChanged }: CurrencyChangeModalProps) {
  const [preview, setPreview] = useState<CurrencyChangePreviewResponse | null>(null)
  const [manualRates, setManualRates] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    let cancelled = false
    previewCurrencyChange(toCurrency)
      .then((p) => !cancelled && setPreview(p))
      .catch((err) => !cancelled && setError(err instanceof Error ? err.message : 'Failed to check the currency change'))
    return () => {
      cancelled = true
    }
  }, [toCurrency])

  const needsRate = preview?.currencies.filter((c) => c.indicativeRate === null) ?? []
  const hasRecords = !!preview && preview.transactionCount + preview.invoiceCount > 0

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    const rates: Record<string, number> = {}
    for (const c of needsRate) {
      const value = Number(manualRates[c.currency])
      if (!value || value <= 0) {
        setError(`Enter the rate from ${c.currency} to ${toCurrency}.`)
        return
      }
      rates[c.currency] = value
    }
    setIsSaving(true)
    try {
      await updateCompanyCurrency(toCurrency, needsRate.length ? rates : undefined)
      await onChanged()
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update currency')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4" role="dialog" aria-modal="true" aria-labelledby="currency-change-title">
      <form onSubmit={handleSubmit} className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 id="currency-change-title" className="text-lg font-bold text-slate-900">
            Change currency to {toCurrency}?
          </h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600" aria-label="Close">
            <X size={20} />
          </button>
        </div>

        {!preview && !error && <p className="text-sm text-slate-500">Checking your records…</p>}

        {preview && !hasRecords && (
          <p className="text-sm text-slate-600">Every amount will be shown in {toCurrency} from now on.</p>
        )}

        {preview && hasRecords && (
          <div className="flex flex-col gap-3 text-sm text-slate-600">
            <p>
              Your {records(preview.transactionCount, preview.invoiceCount)} will be converted from {fromCurrency} totals into {toCurrency}.
              Each one is converted from its original currency at the rate on its own date (today's rate where no history is
              available), so your balance and reports stay accurate.
            </p>
            {preview.currencies.length > 0 && (
              <ul className="flex flex-col gap-2" data-testid="currency-change-rates">
                {preview.currencies.map((c) => (
                  <li key={c.currency} className="flex items-center justify-between gap-3 rounded-lg bg-slate-50 px-3 py-2">
                    <span>
                      <span className="font-medium text-slate-900">{c.currency}</span>{' '}
                      <span className="text-xs text-slate-400">{records(c.transactionCount, c.invoiceCount)}</span>
                    </span>
                    {c.indicativeRate !== null ? (
                      <span className="text-xs text-slate-500">
                        today 1 {c.currency} = {c.indicativeRate} {toCurrency}
                      </span>
                    ) : (
                      <input
                        type="number"
                        step="0.000001"
                        min="0"
                        required
                        aria-label={`Rate from ${c.currency} to ${toCurrency}`}
                        placeholder={`1 ${c.currency} = ? ${toCurrency}`}
                        value={manualRates[c.currency] ?? ''}
                        onChange={(e) => setManualRates((r) => ({ ...r, [c.currency]: e.target.value }))}
                        className="w-40 rounded-lg border border-slate-200 px-2 py-1 text-sm focus:border-brand-primary focus:outline-none"
                      />
                    )}
                  </li>
                ))}
              </ul>
            )}
            {needsRate.length > 0 && (
              <p className="text-xs text-amber-600">No live rate is available for {needsRate.map((c) => c.currency).join(', ')}. Enter the rate to use.</p>
            )}
          </div>
        )}

        {error && <p className="mt-3 text-sm text-red-500">{error}</p>}

        <div className="mt-6 flex justify-end gap-2">
          <button type="button" onClick={onClose} className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">
            Cancel
          </button>
          <button
            type="submit"
            disabled={!preview || isSaving}
            className="rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:opacity-90 disabled:opacity-60"
          >
            {isSaving ? 'Converting…' : hasRecords ? `Convert to ${toCurrency}` : `Use ${toCurrency}`}
          </button>
        </div>
      </form>
    </div>
  )
}
