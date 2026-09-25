import { ChevronLeft, ChevronRight, Plus, Upload } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { categoryColor, categoryLabel, formatCurrency, formatDate } from '../../lib/categoryDisplay'
import { fetchTransactions } from '../../lib/transactionsApi'
import type { PagedResult, TransactionResponse } from '../../lib/types'
import { AddTransactionModal } from './AddTransactionModal'

const PAGE_SIZE = 10

export function TransactionsPage() {
  const [page, setPage] = useState(1)
  const [reloadToken, setReloadToken] = useState(0)
  const [result, setResult] = useState<PagedResult<TransactionResponse> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [showAddModal, setShowAddModal] = useState(false)

  useEffect(() => {
    let cancelled = false
    fetchTransactions(page, PAGE_SIZE)
      .then((r) => !cancelled && setResult(r))
      .catch(() => !cancelled && setResult(null))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [page, reloadToken])

  function goToPage(next: number) {
    setIsLoading(true)
    setPage(next)
  }

  function handleCreated() {
    setShowAddModal(false)
    setIsLoading(true)
    setPage(1)
    setReloadToken((t) => t + 1)
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Transactions</h1>
          <p className="text-sm text-slate-500">All income and expenses for your company.</p>
        </div>
        <div className="flex items-center gap-2">
          <Link
            to="/transactions/import"
            className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            <Upload size={16} />
            Import CSV
          </Link>
          <button
            type="button"
            onClick={() => setShowAddModal(true)}
            className="flex items-center gap-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
          >
            <Plus size={16} />
            Add Transaction
          </button>
        </div>
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
              <th className="px-5 py-3 font-medium">Date</th>
              <th className="px-5 py-3 font-medium">Description</th>
              <th className="px-5 py-3 font-medium">Category</th>
              <th className="px-5 py-3 font-medium">Amount</th>
              <th className="px-5 py-3 font-medium">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              <tr>
                <td colSpan={5} className="px-5 py-8 text-center text-slate-400">
                  Loading…
                </td>
              </tr>
            ) : !result || result.items.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-5 py-8 text-center text-slate-400">
                  No transactions yet. Add your first one.
                </td>
              </tr>
            ) : (
              result.items.map((tx) => (
                <tr key={tx.id}>
                  <td className="px-5 py-3 text-slate-500">{formatDate(tx.transactionDateUtc)}</td>
                  <td className="px-5 py-3 font-medium text-slate-900">{tx.description}</td>
                  <td className="px-5 py-3">
                    <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${categoryColor(tx.category)}`}>
                      {categoryLabel(tx.category)}
                    </span>
                  </td>
                  <td className={`px-5 py-3 font-semibold ${tx.amount > 0 ? 'text-emerald-600' : 'text-red-500'}`}>
                    {tx.amount > 0 ? '+' : '-'}
                    {formatCurrency(Math.abs(tx.amount), tx.currency)}
                  </td>
                  <td className="px-5 py-3">
                    <span className="inline-block rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-600">
                      {tx.status}
                    </span>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {result && result.totalCount > 0 && (
          <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3">
            <p className="text-xs text-slate-500">
              Page {result.pageNumber} of {result.totalPages} · {result.totalCount} total
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={!result.hasPreviousPage}
                onClick={() => goToPage(page - 1)}
                className="rounded-lg border border-slate-200 p-1.5 text-slate-500 disabled:opacity-40"
              >
                <ChevronLeft size={16} />
              </button>
              <button
                type="button"
                disabled={!result.hasNextPage}
                onClick={() => goToPage(page + 1)}
                className="rounded-lg border border-slate-200 p-1.5 text-slate-500 disabled:opacity-40"
              >
                <ChevronRight size={16} />
              </button>
            </div>
          </div>
        )}
      </div>

      {showAddModal && <AddTransactionModal onClose={() => setShowAddModal(false)} onCreated={handleCreated} />}
    </div>
  )
}
