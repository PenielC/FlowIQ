import { Download } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import { InvoiceDocument } from '../../components/InvoiceDocument'
import { fetchPublicInvoice } from '../../lib/invoicesApi'
import { isIOS, renderInvoicePdf, saveFile, shareOrOpenPdf } from '../../lib/invoicePdf'
import type { PublicInvoiceResponse } from '../../lib/types'

/** What a customer sees from "View invoice" in their email: the invoice, and a PDF to keep. No sign-in. */
export function PublicInvoicePage() {
  const { token = '' } = useParams()
  const [invoice, setInvoice] = useState<PublicInvoiceResponse | null>(null)
  const [failed, setFailed] = useState(false)
  const [isDownloading, setIsDownloading] = useState(false)
  const [readyPdf, setReadyPdf] = useState<File | null>(null)
  const documentRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    let cancelled = false
    fetchPublicInvoice(token)
      .then((i) => !cancelled && setInvoice(i))
      .catch(() => !cancelled && setFailed(true))
    return () => {
      cancelled = true
    }
  }, [token])

  useEffect(() => {
    if (invoice) document.title = `${invoice.invoiceNumber} from ${invoice.businessName}`
  }, [invoice])

  async function download() {
    if (!documentRef.current || !invoice) return
    if (readyPdf) {
      await shareOrOpenPdf(readyPdf)
      return
    }
    setIsDownloading(true)
    try {
      const file = await renderInvoicePdf(documentRef.current, `${invoice.invoiceNumber}.pdf`)
      if (!isIOS()) saveFile(file)
      else if (!(await shareOrOpenPdf(file))) setReadyPdf(file)
    } finally {
      setIsDownloading(false)
    }
  }

  return (
    <div className="min-h-screen bg-slate-100 px-4 py-8 print:bg-white print:p-0">
      <div className="mx-auto w-full max-w-2xl">
        {failed ? (
          <div className="rounded-2xl bg-white p-8 text-center shadow-sm" data-testid="public-invoice-invalid">
            <h1 className="text-lg font-semibold text-slate-900">This invoice link isn’t valid</h1>
            <p className="mt-2 text-sm text-slate-500">Please ask the business that sent it for a new link.</p>
          </div>
        ) : !invoice ? (
          <p className="py-16 text-center text-sm text-slate-400">Loading…</p>
        ) : (
          <>
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3 print:hidden">
              <p className="text-sm text-slate-500">
                Invoice from <span className="font-medium text-slate-800">{invoice.businessName}</span>
              </p>
              <button
                type="button"
                onClick={download}
                disabled={isDownloading}
                className="flex items-center gap-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
              >
                <Download size={16} />
                {isDownloading ? 'Preparing…' : readyPdf ? 'Save PDF' : 'Download PDF'}
              </button>
            </div>
            <div className="rounded-2xl bg-white p-5 shadow-sm sm:p-8 print:rounded-none print:shadow-none" data-testid="public-invoice">
              <InvoiceDocument
                ref={documentRef}
                businessName={invoice.businessName}
                logoDataUrl={invoice.logoDataUrl}
                number={invoice.invoiceNumber}
                status={invoice.status}
                customerName={invoice.customerName}
                issueDateUtc={invoice.issueDateUtc}
                dueDateUtc={invoice.dueDateUtc}
                lineItems={invoice.lineItems}
                currency={invoice.currency}
                amount={invoice.amount}
                notes={invoice.notes}
              />
            </div>
            <p className="mt-4 text-center text-xs text-slate-400 print:hidden">Sent with FinFlow</p>
          </>
        )}
      </div>
    </div>
  )
}
