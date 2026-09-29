import { useState } from 'react'
import { repairCurrencyData } from '../../lib/adminApi'
import type { RepairCurrencyDataResponse } from '../../lib/types'

/**
 * One-off repair of data written before the currency fixes: foreign-currency records saved at a silent 1:1 rate or
 * relabelled by a currency change, and (optionally) paid invoices that never recorded income. Always check first.
 */
export function CurrencyRepairCard() {
  const [backfill, setBackfill] = useState(false)
  const [result, setResult] = useState<RepairCurrencyDataResponse | null>(null)
  const [checkedWith, setCheckedWith] = useState<boolean | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isRunning, setIsRunning] = useState(false)

  async function run(dryRun: boolean) {
    setError(null)
    setIsRunning(true)
    try {
      const r = await repairCurrencyData(dryRun, backfill)
      setResult(r)
      setCheckedWith(dryRun ? backfill : null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Repair failed')
    } finally {
      setIsRunning(false)
    }
  }

  const total = result ? result.transactionsRestated + result.invoicesRestated + result.incomeRecorded : 0

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-6" data-testid="currency-repair">
      <h2 className="text-base font-semibold text-slate-900">Currency data repair</h2>
      <p className="mt-1 text-sm text-slate-500">
        Fixes records saved before 29 Sep 2026: foreign-currency amounts stored at a 1:1 rate, and amounts relabelled when a
        company changed its currency. Each is converted from its original currency at the rate on its own date.
      </p>
      <label className="mt-4 flex items-start gap-2 text-sm text-slate-700">
        <input type="checkbox" checked={backfill} onChange={(e) => setBackfill(e.target.checked)} className="mt-1" />
        <span>
          Also record income for invoices already marked paid.{' '}
          <span className="text-slate-400">Skip this if companies also logged those payments as transactions, or revenue will be counted twice.</span>
        </span>
      </label>
      <div className="mt-4 flex gap-2">
        <button
          type="button"
          onClick={() => run(true)}
          disabled={isRunning}
          className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-60"
        >
          {isRunning ? 'Working…' : 'Check (dry run)'}
        </button>
        <button
          type="button"
          onClick={() => run(false)}
          // Only after a dry run with the same options, so nobody repairs blind.
          disabled={isRunning || checkedWith !== backfill || total === 0}
          className="rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:opacity-90 disabled:opacity-40"
        >
          Repair now
        </button>
      </div>
      {error && <p className="mt-3 text-sm text-red-500">{error}</p>}
      {result && (
        <div className="mt-4 text-sm">
          <p className="font-medium text-slate-900" data-testid="repair-summary">
            {result.dryRun ? 'Would change' : 'Changed'}: {result.transactionsRestated} transactions, {result.invoicesRestated} invoices restated
            {backfill ? `, ${result.incomeRecorded} paid invoices recorded as income` : ''}.
          </p>
          {result.companies.length > 0 && (
            <ul className="mt-2 flex flex-col gap-1 text-slate-600">
              {result.companies.map((c) => (
                <li key={c.companyId}>
                  {c.companyName} ({c.currency}): {c.transactionsRestated} transactions, {c.invoicesRestated} invoices
                  {c.incomeRecorded ? `, ${c.incomeRecorded} income` : ''}
                  {c.problem && <span className="text-amber-600"> · {c.problem}</span>}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}
