import { Link } from 'react-router-dom'
import { formatCurrency, formatDate } from '../../lib/categoryDisplay'
import { invoiceStatusColor, invoiceStatusLabel } from '../../lib/invoiceDisplay'
import type { InvoiceResponse } from '../../lib/types'

export function UpcomingInvoicesCard({ invoices, isLoading }: { invoices: InvoiceResponse[]; isLoading: boolean }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-900">Upcoming Invoices</h2>
        <Link to="/invoices" className="text-xs font-medium text-brand-primary hover:underline">
          View all
        </Link>
      </div>

      {isLoading ? (
        <p className="py-6 text-center text-sm text-slate-400">Loading…</p>
      ) : invoices.length === 0 ? (
        <p className="py-6 text-center text-sm text-slate-400">No upcoming invoices.</p>
      ) : (
        <>
          <ul className="divide-y divide-slate-100 sm:hidden">
            {invoices.map((invoice) => (
              <li key={invoice.id} className="py-3">
                <div className="flex items-center justify-between gap-3">
                  <span className="min-w-0 truncate font-medium text-slate-900">{invoice.customerName}</span>
                  <span className="shrink-0 text-sm font-semibold text-slate-900">{formatCurrency(invoice.amount, invoice.currency)}</span>
                </div>
                <div className="mt-1 flex items-center justify-between gap-3">
                  <span className="text-xs text-slate-500">Due {formatDate(invoice.dueDateUtc)}</span>
                  <span className={`inline-block rounded-full px-2 py-0.5 text-[11px] font-medium ${invoiceStatusColor(invoice.status)}`}>
                    {invoiceStatusLabel(invoice.status)}
                  </span>
                </div>
              </li>
            ))}
          </ul>

          <div className="hidden overflow-x-auto sm:block">
            <table className="w-full whitespace-nowrap text-sm">
              <thead>
                <tr className="text-left text-xs text-slate-500">
                  <th className="pb-3 pr-4 font-medium">Customer</th>
                  <th className="pb-3 pr-4 font-medium">Amount</th>
                  <th className="pb-3 pr-4 font-medium">Due Date</th>
                  <th className="pb-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {invoices.map((invoice) => (
                  <tr key={invoice.id}>
                    <td className="py-3 pr-4 font-medium text-slate-900">{invoice.customerName}</td>
                    <td className="py-3 pr-4 text-slate-700">{formatCurrency(invoice.amount, invoice.currency)}</td>
                    <td className="py-3 pr-4 text-slate-500">{formatDate(invoice.dueDateUtc)}</td>
                    <td className="py-3">
                      <span className={`inline-block rounded-full px-2 py-0.5 text-[11px] font-medium ${invoiceStatusColor(invoice.status)}`}>
                        {invoiceStatusLabel(invoice.status)}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
