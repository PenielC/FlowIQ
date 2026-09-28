import { ChevronLeft, ChevronRight, Eye, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { formatDate } from '../../lib/categoryDisplay'
import { fetchCustomers } from '../../lib/customersApi'
import type { CustomerResponse, PagedResult } from '../../lib/types'
import { CustomerDetailModal } from './CustomerDetailModal'
import { CustomerFormModal } from './CustomerFormModal'

const PAGE_SIZE = 10

export function CustomersPage() {
  const [page, setPage] = useState(1)
  const [reloadToken, setReloadToken] = useState(0)
  const [result, setResult] = useState<PagedResult<CustomerResponse> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [showAddModal, setShowAddModal] = useState(false)
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    fetchCustomers(page, PAGE_SIZE)
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

  function handleSaved() {
    setShowAddModal(false)
    setIsLoading(true)
    setPage(1)
    setReloadToken((t) => t + 1)
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Customers</h1>
          <p className="text-sm text-slate-500">Your customer contact directory.</p>
        </div>
        <button
          type="button"
          onClick={() => setShowAddModal(true)}
          className="flex items-center gap-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
        >
          <Plus size={16} />
          New Customer
        </button>
      </div>

      <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="w-full whitespace-nowrap text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
              <th className="px-5 py-3 font-medium">Name</th>
              <th className="px-5 py-3 font-medium">Email</th>
              <th className="px-5 py-3 font-medium">Phone</th>
              <th className="px-5 py-3 font-medium">Added</th>
              <th className="px-5 py-3 font-medium"></th>
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
                  No customers yet. Add your first one.
                </td>
              </tr>
            ) : (
              result.items.map((customer) => (
                <tr
                  key={customer.id}
                  onClick={() => setSelectedCustomerId(customer.id)}
                  className="cursor-pointer transition-colors hover:bg-slate-50"
                >
                  <td className="px-5 py-3 font-medium text-slate-900">{customer.name}</td>
                  <td className="px-5 py-3 text-slate-500">{customer.email ?? '—'}</td>
                  <td className="px-5 py-3 text-slate-500">{customer.phone ?? '—'}</td>
                  <td className="px-5 py-3 text-slate-500">{formatDate(customer.createdAtUtc)}</td>
                  <td className="px-5 py-3 text-right">
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation()
                        setSelectedCustomerId(customer.id)
                      }}
                      className="flex items-center gap-1 text-xs font-medium text-slate-500 hover:text-slate-700 hover:underline"
                    >
                      <Eye size={14} />
                      View
                    </button>
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

      {showAddModal && <CustomerFormModal onClose={() => setShowAddModal(false)} onSaved={handleSaved} />}

      {selectedCustomerId && (
        <CustomerDetailModal
          customerId={selectedCustomerId}
          onClose={() => setSelectedCustomerId(null)}
          onChanged={() => setReloadToken((t) => t + 1)}
        />
      )}
    </div>
  )
}
