import { AlertTriangle, ArrowLeft, Copy, Upload } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../lib/AuthContext'
import { formatCurrency } from '../../lib/categoryDisplay'
import { currencies } from '../../lib/currencies'
import { fetchExchangeRate } from '../../lib/exchangeRatesApi'
import { RateAttribution } from '../../components/RateAttribution'
import {
  confirmTransactionImport,
  previewTransactionImport,
  type CsvColumnMapping,
} from '../../lib/transactionImportApi'
import { transactionCategories, type TransactionImportRowResponse } from '../../lib/types'

const DATE_FORMATS = [
  { label: 'YYYY-MM-DD (2026-09-25)', value: 'yyyy-MM-dd' },
  { label: 'MM/DD/YYYY (09/25/2026)', value: 'MM/dd/yyyy' },
  { label: 'DD/MM/YYYY (25/09/2026)', value: 'dd/MM/yyyy' },
  { label: 'M/D/YYYY (9/25/2026)', value: 'M/d/yyyy' },
]

function splitCsvLine(line: string): string[] {
  return line.split(',').map((s) => s.trim().replace(/^"|"$/g, ''))
}

interface ReviewRow extends TransactionImportRowResponse {
  included: boolean
  category: string
}

export function ImportTransactionsPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const companyCurrency = user?.companyCurrency ?? 'USD'

  const [step, setStep] = useState<'upload' | 'review'>('upload')
  const [file, setFile] = useState<File | null>(null)
  const [headers, setHeaders] = useState<string[]>([])
  const [sampleRows, setSampleRows] = useState<string[][]>([])
  const [hasHeaderRow, setHasHeaderRow] = useState(true)
  const [dateColumnIndex, setDateColumnIndex] = useState(0)
  const [descriptionColumnIndex, setDescriptionColumnIndex] = useState(1)
  const [amountColumnIndex, setAmountColumnIndex] = useState(2)
  const [dateFormat, setDateFormat] = useState(DATE_FORMATS[0].value)
  const [currency, setCurrency] = useState(companyCurrency)
  const [exchangeRate, setExchangeRate] = useState('')
  const [rateIsLive, setRateIsLive] = useState(true)
  const [rateSource, setRateSource] = useState<string | null>(null)
  const [isFetchingRate, setIsFetchingRate] = useState(false)

  const [reviewRows, setReviewRows] = useState<ReviewRow[]>([])
  const [resolvedRate, setResolvedRate] = useState(1)
  const [isLoadingPreview, setIsLoadingPreview] = useState(false)
  const [isImporting, setIsImporting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleFileSelected(selected: File) {
    setFile(selected)
    setError(null)
    const text = await selected.text()
    const lines = text.split(/\r?\n/).filter((l) => l.trim().length > 0).slice(0, 6)
    const parsed = lines.map(splitCsvLine)
    setSampleRows(parsed)
    setHeaders(parsed[0] ?? [])
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

  const columnOptions = (headers.length > 0 ? headers : (sampleRows[0] ?? [])).map((_, i) => ({
    index: i,
    label: hasHeaderRow && headers[i] ? headers[i] : `Column ${i + 1}`,
    sample: sampleRows[hasHeaderRow ? 1 : 0]?.[i] ?? '',
  }))

  async function handlePreview() {
    if (!file) return
    setError(null)
    setIsLoadingPreview(true)
    try {
      const mapping: CsvColumnMapping = { dateColumnIndex, descriptionColumnIndex, amountColumnIndex, dateFormat, hasHeaderRow }
      const numericRate = exchangeRate ? Number(exchangeRate) : undefined
      const result = await previewTransactionImport(file, mapping, currency, currency === companyCurrency ? undefined : numericRate)
      setReviewRows(
        result.rows.map((r) => ({
          ...r,
          included: r.parseError === null && !r.isDuplicate,
          category: r.suggestedCategory,
        })),
      )
      setResolvedRate(result.exchangeRate)
      setStep('review')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to preview import')
    } finally {
      setIsLoadingPreview(false)
    }
  }

  function updateRow(rowNumber: number, patch: Partial<ReviewRow>) {
    setReviewRows((rows) => rows.map((r) => (r.rowNumber === rowNumber ? { ...r, ...patch } : r)))
  }

  const includedCount = reviewRows.filter((r) => r.included).length

  async function handleImport() {
    setError(null)
    setIsImporting(true)
    try {
      const rows = reviewRows
        .filter((r) => r.included && r.transactionDateUtc && r.amount !== null)
        .map((r) => ({
          transactionDateUtc: r.transactionDateUtc as string,
          description: r.description,
          amount: r.amount as number,
          category: r.category,
        }))
      await confirmTransactionImport(rows, currency, resolvedRate)
      navigate('/transactions')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to import transactions')
    } finally {
      setIsImporting(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center gap-3">
        <button type="button" onClick={() => navigate('/transactions')} className="text-slate-400 hover:text-slate-600">
          <ArrowLeft size={20} />
        </button>
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Import Transactions</h1>
          <p className="text-sm text-slate-500">Upload a CSV file to bring in your transaction history.</p>
        </div>
      </div>

      {error && <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>}

      {step === 'upload' && (
        <div className="flex flex-col gap-5 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">CSV File</label>
            <input
              type="file"
              accept=".csv,text/csv"
              onChange={(e) => e.target.files?.[0] && handleFileSelected(e.target.files[0])}
              className="block w-full text-sm text-slate-600 file:mr-4 file:rounded-lg file:border-0 file:bg-brand-primary file:px-4 file:py-2 file:text-sm file:font-semibold file:text-white hover:file:bg-emerald-700"
            />
          </div>

          {sampleRows.length > 0 && (
            <>
              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input type="checkbox" checked={hasHeaderRow} onChange={(e) => setHasHeaderRow(e.target.checked)} />
                First row is a header row
              </label>

              <div className="overflow-x-auto rounded-lg border border-slate-100">
                <table className="w-full text-xs">
                  <tbody>
                    {sampleRows.slice(0, 4).map((row, i) => (
                      <tr key={i} className={i === 0 && hasHeaderRow ? 'bg-slate-50 font-medium' : ''}>
                        {row.map((cell, j) => (
                          <td key={j} className="border-b border-slate-100 px-3 py-1.5 text-slate-600">
                            {cell}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Date column</label>
                  <select
                    value={dateColumnIndex}
                    onChange={(e) => setDateColumnIndex(Number(e.target.value))}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
                  >
                    {columnOptions.map((c) => (
                      <option key={c.index} value={c.index}>
                        {c.label} {c.sample && `(e.g. "${c.sample}")`}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Description column</label>
                  <select
                    value={descriptionColumnIndex}
                    onChange={(e) => setDescriptionColumnIndex(Number(e.target.value))}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
                  >
                    {columnOptions.map((c) => (
                      <option key={c.index} value={c.index}>
                        {c.label} {c.sample && `(e.g. "${c.sample}")`}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Amount column</label>
                  <select
                    value={amountColumnIndex}
                    onChange={(e) => setAmountColumnIndex(Number(e.target.value))}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
                  >
                    {columnOptions.map((c) => (
                      <option key={c.index} value={c.index}>
                        {c.label} {c.sample && `(e.g. "${c.sample}")`}
                      </option>
                    ))}
                  </select>
                  <p className="mt-1 text-xs text-slate-400">
                    One signed amount column (negative = expense, positive = income).
                  </p>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Date format</label>
                  <select
                    value={dateFormat}
                    onChange={(e) => setDateFormat(e.target.value)}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
                  >
                    {DATE_FORMATS.map((f) => (
                      <option key={f.value} value={f.value}>
                        {f.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Currency</label>
                  <select
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
                    <label className="mb-1 block text-sm font-medium text-slate-700">Exchange rate to {companyCurrency}</label>
                    <input
                      type="number"
                      step="0.000001"
                      min="0"
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

              <button
                type="button"
                onClick={handlePreview}
                disabled={isLoadingPreview}
                className="flex w-fit items-center gap-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
              >
                <Upload size={16} />
                {isLoadingPreview ? 'Analyzing…' : 'Preview Import'}
              </button>
            </>
          )}
        </div>
      )}

      {step === 'review' && (
        <div className="flex flex-col gap-4">
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 shadow-sm">
            {includedCount} of {reviewRows.length} rows will be imported.
          </div>

          <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-sm">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
                  <th className="px-4 py-3"></th>
                  <th className="px-4 py-3 font-medium">Date</th>
                  <th className="px-4 py-3 font-medium">Description</th>
                  <th className="px-4 py-3 font-medium">Amount</th>
                  <th className="px-4 py-3 font-medium">Category</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {reviewRows.map((row) => (
                  <tr key={row.rowNumber} className={row.parseError ? 'bg-red-50/50' : row.isDuplicate ? 'bg-amber-50/50' : ''}>
                    <td className="px-4 py-2.5">
                      <input
                        type="checkbox"
                        checked={row.included}
                        disabled={!!row.parseError}
                        onChange={(e) => updateRow(row.rowNumber, { included: e.target.checked })}
                      />
                    </td>
                    <td className="px-4 py-2.5 text-slate-600">
                      {row.transactionDateUtc ? new Date(row.transactionDateUtc).toLocaleDateString() : '—'}
                    </td>
                    <td className="px-4 py-2.5 text-slate-900">{row.description || '—'}</td>
                    <td className="px-4 py-2.5 text-slate-600">
                      {row.amount !== null ? formatCurrency(row.amount, currency) : '—'}
                    </td>
                    <td className="px-4 py-2.5">
                      <select
                        value={row.category}
                        disabled={!!row.parseError}
                        onChange={(e) => updateRow(row.rowNumber, { category: e.target.value })}
                        className="rounded-lg border border-slate-200 px-2 py-1 text-xs focus:border-brand-primary focus:outline-none"
                      >
                        {transactionCategories.map((c) => (
                          <option key={c} value={c}>
                            {c}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="px-4 py-2.5">
                      {row.parseError ? (
                        <span className="flex items-center gap-1 text-xs font-medium text-red-600">
                          <AlertTriangle size={12} />
                          {row.parseError}
                        </span>
                      ) : row.isDuplicate ? (
                        <span className="flex items-center gap-1 text-xs font-medium text-amber-600">
                          <Copy size={12} />
                          Possible duplicate
                        </span>
                      ) : (
                        <span className="text-xs text-emerald-600">Ready</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex gap-3">
            <button
              type="button"
              onClick={() => setStep('upload')}
              className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              Back
            </button>
            <button
              type="button"
              onClick={handleImport}
              disabled={isImporting || includedCount === 0}
              className="rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
            >
              {isImporting ? 'Importing…' : `Import ${includedCount} Transaction${includedCount === 1 ? '' : 's'}`}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
