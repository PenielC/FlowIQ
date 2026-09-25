import { CheckCircle2, ChevronLeft, ChevronRight, Eye, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { formatCurrency, formatDate } from '../../lib/categoryDisplay'
import { fetchInvoices, markInvoicePaid } from '../../lib/invoicesApi'
import { invoiceStatusColor, invoiceStatusLabel } from '../../lib/invoiceDisplay'
import type { InvoiceResponse, PagedResult } from '../../lib/types'
import { AddInvoiceModal } from './AddInvoiceModal'
import { InvoiceDetailModal } from './InvoiceDetailModal'

const PAGE_SIZE = 10

export function InvoicesPage() {
  const [page, setPage] = useState(1)
  const [reloadToken, setReloadToken] = useState(0)
  const [result, setResult] = useState<PagedResult<InvoiceResponse> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [showAddModal, setShowAddModal] = useState(false)
  const [payingId, setPayingId] = useState<string | null>(null)
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    fetchInvoices(page, PAGE_SIZE)
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

  async function handleMarkPaid(id: string) {
    setPayingId(id)
    try {
      await markInvoicePaid(id)
      setReloadToken((t) => t + 1)
    } catch {
      // Swallow — the row simply won't update; a toast/error UI can be added later.
    } finally {
      setPayingId(null)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Invoices</h1>
          <p className="text-sm text-slate-500">Track what your customers owe you.</p>
        </div>
        <button
          type="button"
          onClick={() => setShowAddModal(true)}
          className="flex items-center gap-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
        >
          <Plus size={16} />
          New Invoice
        </button>
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
              <th className="px-5 py-3 font-medium">Customer</th>
              <th className="px-5 py-3 font-medium">Amount</th>
              <th className="px-5 py-3 font-medium">Issue Date</th>
              <th className="px-5 py-3 font-medium">Due Date</th>
              <th className="px-5 py-3 font-medium">Status</th>
              <th className="px-5 py-3 font-medium"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              <tr>
                <td colSpan={6} className="px-5 py-8 text-center text-slate-400">
                  Loading…
                </td>
              </tr>
            ) : !result || result.items.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-5 py-8 text-center text-slate-400">
                  No invoices yet. Create your first one.
                </td>
              </tr>
            ) : (
              result.items.map((invoice) => (
                <tr
                  key={invoice.id}
                  onClick={() => setSelectedInvoiceId(invoice.id)}
                  className="cursor-pointer transition-colors hover:bg-slate-50"
                >
                  <td className="px-5 py-3 font-medium text-slate-900">{invoice.customerName}</td>
                  <td className="px-5 py-3 text-slate-700">{formatCurrency(invoice.amount, invoice.currency)}</td>
                  <td className="px-5 py-3 text-slate-500">{formatDate(invoice.issueDateUtc)}</td>
                  <td className="px-5 py-3 text-slate-500">{formatDate(invoice.dueDateUtc)}</td>
                  <td className="px-5 py-3">
                    <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${invoiceStatusColor(invoice.status)}`}>
                      {invoiceStatusLabel(invoice.status)}
                    </span>
                  </td>
                  <td className="px-5 py-3">
                    <div className="flex items-center justify-end gap-4">
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation()
                          setSelectedInvoiceId(invoice.id)
                        }}
                        className="flex items-center gap-1 text-xs font-medium text-slate-500 hover:text-slate-700 hover:underline"
                      >
                        <Eye size={14} />
                        View
                      </button>
                      {invoice.status !== 'Paid' && (
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation()
                            handleMarkPaid(invoice.id)
                          }}
                          disabled={payingId === invoice.id}
                          className="flex items-center gap-1 text-xs font-medium text-emerald-600 hover:underline disabled:opacity-50"
                        >
                          <CheckCircle2 size={14} />
                          {payingId === invoice.id ? 'Marking…' : 'Mark Paid'}
                        </button>
                      )}
                    </div>
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

      {showAddModal && <AddInvoiceModal onClose={() => setShowAddModal(false)} onCreated={handleCreated} />}

      {selectedInvoiceId && (
        <InvoiceDetailModal
          invoiceId={selectedInvoiceId}
          onClose={() => setSelectedInvoiceId(null)}
          onChanged={() => setReloadToken((t) => t + 1)}
        />
      )}
    </div>
  )
}
