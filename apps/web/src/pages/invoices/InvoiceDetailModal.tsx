import { CheckCircle2, Download, Printer, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { fetchCompanyLogo } from '../../lib/companiesApi'
import { useAuth } from '../../lib/AuthContext'
import { fetchInvoiceById, markInvoicePaid } from '../../lib/invoicesApi'
import { isIOS, renderInvoicePdf, saveFile, shareOrOpenPdf } from '../../lib/invoicePdf'
import { invoiceNumber } from '../../lib/invoiceDisplay'
import type { InvoiceResponse } from '../../lib/types'
import { InvoiceDocument } from '../../components/InvoiceDocument'
import { InvoiceDeliveryPanel } from './InvoiceDeliveryPanel'

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
  const [readyPdf, setReadyPdf] = useState<File | null>(null)
  const documentRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    document.body.classList.add('invoice-modal-open')
    return () => document.body.classList.remove('invoice-modal-open')
  }, [])

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
      setReadyPdf(null)
      onChanged()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to mark invoice as paid')
    } finally {
      setIsMarkingPaid(false)
    }
  }

  async function handleDownload() {
    if (!documentRef.current || !invoice) return
    setError(null)

    // Second tap on iOS: the PDF is already built, so hand it over inside this fresh tap.
    if (readyPdf) {
      if (!(await shareOrOpenPdf(readyPdf))) setError('Your browser blocked the PDF. Allow pop-ups for this site and tap Save PDF again.')
      return
    }

    setIsDownloading(true)
    try {
      const file = await renderInvoicePdf(documentRef.current, `${invoiceNumber(invoice.id)}.pdf`)
      if (!isIOS()) {
        saveFile(file)
      } else if (!(await shareOrOpenPdf(file))) {
        setReadyPdf(file)
      }
    } catch {
      setError('Could not create the PDF. Please try again, or use Print and choose Save as PDF.')
    } finally {
      setIsDownloading(false)
    }
  }

  return createPortal(
    <div className="invoice-print-overlay fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 px-4 py-8 print:bg-white print:p-0">
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
          <div className="p-5 sm:p-8">
            <InvoiceDocument
              ref={documentRef}
              businessName={user?.companyName ?? ''}
              logoDataUrl={logoDataUrl}
              number={invoiceNumber(invoice.id)}
              status={invoice.status}
              customerName={invoice.customerName}
              issueDateUtc={invoice.issueDateUtc}
              dueDateUtc={invoice.dueDateUtc}
              lineItems={invoice.lineItems}
              currency={invoice.currency}
              amount={invoice.amount}
              notes={invoice.notes}
            />

            {error && <p className="mt-4 text-sm text-red-500 print:hidden">{error}</p>}
            {readyPdf && !error && (
              <p className="mt-4 text-sm text-emerald-700 print:hidden">Your PDF is ready. Tap Save PDF to save or share it.</p>
            )}

            <div className="mt-8 flex flex-wrap items-center gap-3 border-t border-slate-100 pt-6 print:hidden">
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
                {isDownloading ? 'Preparing…' : readyPdf ? 'Save PDF' : 'Download'}
              </button>
            </div>

            <InvoiceDeliveryPanel
              invoice={invoice}
              onUpdated={(updated) => {
                setInvoice(updated)
                onChanged()
              }}
            />
          </div>
        ) : null}
      </div>
    </div>,
    document.body,
  )
}
