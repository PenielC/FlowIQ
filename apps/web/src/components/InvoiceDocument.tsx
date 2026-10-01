import { forwardRef } from 'react'
import { formatCurrency, formatDate } from '../lib/categoryDisplay'
import { dueDateNote, invoiceStatusColor, invoiceStatusLabel } from '../lib/invoiceDisplay'
import type { InvoiceLineItemResponse } from '../lib/types'

export interface InvoiceDocumentProps {
  businessName: string
  logoDataUrl: string | null
  number: string
  status: string
  customerName: string
  issueDateUtc: string
  dueDateUtc: string
  lineItems: InvoiceLineItemResponse[]
  currency: string
  amount: number
  notes: string | null
}

/** The printable invoice, as the business sees it and as the customer sees it from their email link. */
export const InvoiceDocument = forwardRef<HTMLDivElement, InvoiceDocumentProps>(function InvoiceDocument(props, ref) {
  const note = dueDateNote(props)
  return (
    <div ref={ref} className="bg-white p-2">
      <div className="flex items-start justify-between gap-6">
        <div className="flex flex-col gap-2">
          {props.logoDataUrl && <img src={props.logoDataUrl} alt="Company logo" className="h-12 w-auto max-w-[160px] object-contain" />}
          <p className="text-lg font-bold text-slate-900">{props.businessName}</p>
        </div>
        <div className="text-right">
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">INVOICE</h1>
          <p className="mt-1 text-sm text-slate-500">{props.number}</p>
          <span className={`mt-2 inline-block rounded-full px-2 py-0.5 text-xs font-medium ${invoiceStatusColor(props.status)}`}>
            {invoiceStatusLabel(props.status)}
          </span>
        </div>
      </div>

      <div className="mt-8 flex items-start justify-between gap-6 border-t border-slate-100 pt-6">
        <div>
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Bill To</p>
          <p className="mt-1 text-base font-semibold text-slate-900">{props.customerName}</p>
        </div>
        <div className="text-right text-sm">
          <p className="text-slate-500">
            Issue Date <span className="ml-2 font-medium text-slate-900">{formatDate(props.issueDateUtc)}</span>
          </p>
          <p className="mt-1 text-slate-500">
            Due Date <span className="ml-2 font-medium text-slate-900">{formatDate(props.dueDateUtc)}</span>
          </p>
          {note && <p className={`mt-1 font-medium ${note.tone}`}>{note.text}</p>}
        </div>
      </div>

      <table className="mt-8 w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 text-left text-xs uppercase tracking-wide text-slate-400">
            <th className="pb-2 font-medium">Description</th>
            <th className="pb-2 text-right font-medium">Amount</th>
          </tr>
        </thead>
        <tbody>
          {props.lineItems.map((li) => (
            <tr key={li.id} className="border-b border-slate-100">
              <td className="py-3 text-slate-700">{li.description}</td>
              <td className="py-3 text-right text-slate-700">{formatCurrency(li.amount, props.currency)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="mt-4 flex justify-end">
        <div className="flex w-full max-w-[240px] items-center justify-between border-t-2 border-slate-900 pt-3">
          <p className="text-base font-bold text-slate-900">Total</p>
          <p className="text-xl font-bold text-slate-900">{formatCurrency(props.amount, props.currency)}</p>
        </div>
      </div>

      {props.notes && (
        <div className="mt-6 border-t border-slate-100 pt-4">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Notes / Terms</p>
          <p className="mt-1 whitespace-pre-wrap text-sm text-slate-600">{props.notes}</p>
        </div>
      )}
    </div>
  )
})
