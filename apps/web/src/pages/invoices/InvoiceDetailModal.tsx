import { CheckCircle2, Download, Printer, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { fetchCompanyLogo } from '../../lib/companiesApi'
import { useAuth } from '../../lib/AuthContext'
import { formatCurrency, formatDate } from '../../lib/categoryDisplay'
import { fetchInvoiceById, markInvoicePaid } from '../../lib/invoicesApi'
import { invoiceStatusColor, invoiceStatusLabel } from '../../lib/invoiceDisplay'
import type { InvoiceResponse } from '../../lib/types'

function dueDateNote(invoice: InvoiceResponse) {
  if (invoice.status === 'Paid') return null

  const daysUntilDue = Math.ceil((new Date(invoice.dueDateUtc).getTime() - Date.now()) / 86400000)
  if (daysUntilDue < 0) return { text: `${Math.abs(daysUntilDue)} day${Math.abs(daysUntilDue) === 1 ? '' : 's'} overdue`, tone: 'text-red-600' }
  if (daysUntilDue === 0) return { text: 'Due today', tone: 'text-amber-600' }
  return { text: `Due in ${daysUntilDue} day${daysUntilDue === 1 ? '' : 's'}`, tone: 'text-slate-500' }
}

function invoiceNumber(id: string) {
  return `INV-${id.slice(0, 8).toUpperCase()}`
}

export function InvoiceDetailModal({
  invoiceId,
  onClose,
  onChanged,
}: {
  invoiceId: string
  onClose: () => void
  onChanged: () => void
}) {
  const { user } = useAuth()
  const [invoice, setInvoice] = useState<InvoiceResponse | null>(null)
  const [logoDataUrl, setLogoDataUrl] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isMarkingPaid, setIsMarkingPaid] = useState(false)
  const [isDownloading, setIsDownloading] = useState(false)
  const documentRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    let cancelled = false
    Promise.all([fetchInvoiceById(invoiceId), fetchCompanyLogo().catch(() => ({ dataUrl: null }))])
      .then(([invoiceData, logo]) => {
        if (cancelled) return
        setInvoice(invoiceData)
        setLogoDataUrl(logo.dataUrl)
      })
      .catch((err) => !cancelled && setError(err instanceof Error ? err.message : 'Failed to load invoice'))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [invoiceId])

  async function handleMarkPaid() {
    setIsMarkingPaid(true)
    try {
      const updated = await markInvoicePaid(invoiceId)
      setInvoice(updated)
      onChanged()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to mark invoice as paid')
    } finally {
      setIsMarkingPaid(false)
    }
  }

  async function handleDownload() {
    if (!documentRef.current || !invoice) return
    setIsDownloading(true)
    try {
      const [{ default: html2canvas }, { jsPDF }] = await Promise.all([import('html2canvas-pro'), import('jspdf')])
      const canvas = await html2canvas(documentRef.current, { scale: 2, backgroundColor: '#ffffff' })

      const pdf = new jsPDF({ unit: 'pt', format: 'a4' })
      const pageWidth = pdf.internal.pageSize.getWidth()
      const imageHeight = (canvas.height * pageWidth) / canvas.width

      pdf.addImage(canvas.toDataURL('image/png'), 'PNG', 0, 0, pageWidth, imageHeight)
      pdf.save(`${invoiceNumber(invoice.id)}.pdf`)
    } finally {
      setIsDownloading(false)
    }
  }

  const note = invoice ? dueDateNote(invoice) : null

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 px-4 py-8 print:bg-white print:p-0">
      <div className="invoice-print-area w-full max-w-2xl rounded-2xl bg-white shadow-xl print:rounded-none print:shadow-none">
        <div className="flex items-center justify-between border-b border-slate-100 p-4 print:hidden">
          <h2 className="text-sm font-semibold text-slate-500">Invoice</h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            <X size={20} />
          </button>
        </div>

        {isLoading ? (
          <p className="py-16 text-center text-sm text-slate-400">Loading…</p>
        ) : error && !invoice ? (
          <p className="py-16 text-center text-sm text-red-500">{error}</p>
        ) : invoice ? (
          <div className="p-8">
            <div ref={documentRef} className="bg-white p-2">
            <div className="flex items-start justify-between gap-6">
              <div className="flex flex-col gap-2">
                {logoDataUrl && <img src={logoDataUrl} alt="Company logo" className="h-12 w-auto max-w-[160px] object-contain" />}
                <p className="text-lg font-bold text-slate-900">{user?.companyName}</p>
              </div>
              <div className="text-right">
                <h1 className="text-2xl font-bold tracking-tight text-slate-900">INVOICE</h1>
                <p className="mt-1 text-sm text-slate-500">{invoiceNumber(invoice.id)}</p>
                <span className={`mt-2 inline-block rounded-full px-2 py-0.5 text-xs font-medium ${invoiceStatusColor(invoice.status)}`}>
                  {invoiceStatusLabel(invoice.status)}
                </span>
              </div>
            </div>

            <div className="mt-8 flex items-start justify-between gap-6 border-t border-slate-100 pt-6">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Bill To</p>
                <p className="mt-1 text-base font-semibold text-slate-900">{invoice.customerName}</p>
              </div>
              <div className="text-right text-sm">
                <p className="text-slate-500">
                  Issue Date <span className="ml-2 font-medium text-slate-900">{formatDate(invoice.issueDateUtc)}</span>
                </p>
                <p className="mt-1 text-slate-500">
                  Due Date <span className="ml-2 font-medium text-slate-900">{formatDate(invoice.dueDateUtc)}</span>
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
                <tr className="border-b border-slate-100">
                  <td className="py-3 text-slate-700">Services rendered</td>
                  <td className="py-3 text-right text-slate-700">{formatCurrency(invoice.amount, invoice.currency)}</td>
                </tr>
              </tbody>
            </table>

            <div className="mt-4 flex justify-end">
              <div className="flex w-full max-w-[240px] items-center justify-between border-t-2 border-slate-900 pt-3">
                <p className="text-base font-bold text-slate-900">Total</p>
                <p className="text-xl font-bold text-slate-900">{formatCurrency(invoice.amount, invoice.currency)}</p>
              </div>
            </div>
            </div>

            {error && <p className="mt-4 text-sm text-red-500 print:hidden">{error}</p>}

            <div className="mt-8 flex items-center gap-3 border-t border-slate-100 pt-6 print:hidden">
              {invoice.status !== 'Paid' && (
                <button
                  type="button"
                  onClick={handleMarkPaid}
                  disabled={isMarkingPaid}
                  className="flex items-center justify-center gap-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
                >
                  <CheckCircle2 size={16} />
                  {isMarkingPaid ? 'Marking…' : 'Mark as Paid'}
                </button>
              )}
              <button
                type="button"
                onClick={() => window.print()}
                className="flex items-center justify-center gap-2 rounded-lg border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                <Printer size={16} />
                Print
              </button>
              <button
                type="button"
                onClick={handleDownload}
                disabled={isDownloading}
                className="flex items-center justify-center gap-2 rounded-lg border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
              >
                <Download size={16} />
                {isDownloading ? 'Preparing…' : 'Download'}
              </button>
            </div>
          </div>
        ) : null}
      </div>
    </div>
  )
}
