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
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-xs text-slate-500">
              <th className="pb-3 font-medium">Customer</th>
              <th className="pb-3 font-medium">Amount</th>
              <th className="pb-3 font-medium">Due Date</th>
              <th className="pb-3 font-medium">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {invoices.map((invoice) => (
              <tr key={invoice.id}>
                <td className="py-3 font-medium text-slate-900">{invoice.customerName}</td>
                <td className="py-3 text-slate-700">{formatCurrency(invoice.amount, invoice.currency)}</td>
                <td className="py-3 text-slate-500">{formatDate(invoice.dueDateUtc)}</td>
                <td className="py-3">
                  <span className={`inline-block rounded-full px-2 py-0.5 text-[11px] font-medium ${invoiceStatusColor(invoice.status)}`}>
                    {invoiceStatusLabel(invoice.status)}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
