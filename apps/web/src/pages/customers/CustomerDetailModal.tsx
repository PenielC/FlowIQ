import { Mail, Pencil, Phone, StickyNote, Trash2, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { formatDate } from '../../lib/categoryDisplay'
import { deleteCustomer, fetchCustomerById } from '../../lib/customersApi'
import type { CustomerResponse } from '../../lib/types'
import { CustomerFormModal } from './CustomerFormModal'

export function CustomerDetailModal({
  customerId,
  onClose,
  onChanged,
}: {
  customerId: string
  onClose: () => void
  onChanged: () => void
}) {
  const [customer, setCustomer] = useState<CustomerResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)
  const [showEditModal, setShowEditModal] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)

  useEffect(() => {
    let cancelled = false
    fetchCustomerById(customerId)
      .then((data) => !cancelled && setCustomer(data))
      .catch((err) => !cancelled && setError(err instanceof Error ? err.message : 'Failed to load customer'))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [customerId])

  async function handleDelete() {
    setIsDeleting(true)
    try {
      await deleteCustomer(customerId)
      onChanged()
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete customer')
      setIsDeleting(false)
    }
  }

  if (showEditModal && customer) {
    return (
      <CustomerFormModal
        customer={customer}
        onClose={() => setShowEditModal(false)}
        onSaved={() => {
          setShowEditModal(false)
          onChanged()
          fetchCustomerById(customerId).then(setCustomer)
        }}
      />
    )
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 px-4 py-8">
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold text-slate-900">Customer</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            <X size={20} />
          </button>
        </div>

        {isLoading ? (
          <p className="py-8 text-center text-sm text-slate-400">Loading…</p>
        ) : error && !customer ? (
          <p className="py-8 text-center text-sm text-red-500">{error}</p>
        ) : customer ? (
          <div className="flex flex-col gap-5">
            <div>
              <p className="text-xl font-bold text-slate-900">{customer.name}</p>
              <p className="mt-1 text-xs text-slate-400">Added {formatDate(customer.createdAtUtc)}</p>
            </div>

            <div className="flex flex-col gap-3 rounded-xl border border-slate-100 p-4">
              <div className="flex items-center gap-3">
                <Mail size={16} className="text-slate-400" />
                <div>
                  <p className="text-xs text-slate-400">Email</p>
                  <p className="text-sm font-medium text-slate-900">{customer.email ?? '—'}</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <Phone size={16} className="text-slate-400" />
                <div>
                  <p className="text-xs text-slate-400">Phone</p>
                  <p className="text-sm font-medium text-slate-900">{customer.phone ?? '—'}</p>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <StickyNote size={16} className="mt-0.5 text-slate-400" />
                <div>
                  <p className="text-xs text-slate-400">Notes</p>
                  <p className="text-sm font-medium text-slate-900">{customer.notes ?? '—'}</p>
                </div>
              </div>
            </div>

            {error && <p className="text-sm text-red-500">{error}</p>}

            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={() => setShowEditModal(true)}
                className="flex items-center justify-center gap-2 rounded-lg border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                <Pencil size={16} />
                Edit
              </button>

              {confirmingDelete ? (
                <div className="flex items-center gap-2">
                  <span className="text-sm text-slate-500">Delete this customer?</span>
                  <button
                    type="button"
                    onClick={handleDelete}
                    disabled={isDeleting}
                    className="rounded-lg bg-red-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-60"
                  >
                    {isDeleting ? 'Deleting…' : 'Confirm'}
                  </button>
                  <button
                    type="button"
                    onClick={() => setConfirmingDelete(false)}
                    className="text-sm font-medium text-slate-500 hover:text-slate-700"
                  >
                    Cancel
                  </button>
                </div>
              ) : (
                <button
                  type="button"
                  onClick={() => setConfirmingDelete(true)}
                  className="flex items-center justify-center gap-2 rounded-lg border border-slate-200 px-4 py-2 text-sm font-semibold text-red-500 hover:bg-red-50"
                >
                  <Trash2 size={16} />
                  Delete
                </button>
              )}
            </div>
          </div>
        ) : null}
      </div>
    </div>
  )
}
