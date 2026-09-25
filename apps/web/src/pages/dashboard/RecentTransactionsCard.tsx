import { CreditCard } from 'lucide-react'
import { Link } from 'react-router-dom'
import { categoryColor, categoryLabel, formatCurrency, formatDate } from '../../lib/categoryDisplay'
import type { TransactionResponse } from '../../lib/types'

export function RecentTransactionsCard({ transactions, isLoading }: { transactions: TransactionResponse[]; isLoading: boolean }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-900">Recent Transactions</h2>
        <Link to="/transactions" className="text-xs font-medium text-brand-primary hover:underline">
          View all
        </Link>
      </div>

      {isLoading ? (
        <p className="py-6 text-center text-sm text-slate-400">Loading…</p>
      ) : transactions.length === 0 ? (
        <p className="py-6 text-center text-sm text-slate-400">No transactions yet.</p>
      ) : (
        <ul className="flex flex-col gap-4">
          {transactions.map((tx) => (
            <li key={tx.id} className="flex items-center gap-3">
              <span className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg ${categoryColor(tx.category)}`}>
                <CreditCard size={16} />
              </span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium text-slate-900">{tx.description}</p>
                <p className="text-xs text-slate-500">
                  {categoryLabel(tx.category)} · {formatDate(tx.transactionDateUtc)}
                </p>
              </div>
              <div className="text-right">
                <p className={`text-sm font-semibold ${tx.amount > 0 ? 'text-emerald-600' : 'text-red-500'}`}>
                  {tx.amount > 0 ? '+' : '-'}
                  {formatCurrency(Math.abs(tx.amount), tx.currency)}
                </p>
                <span className="inline-block rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-600">
                  {tx.status}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
