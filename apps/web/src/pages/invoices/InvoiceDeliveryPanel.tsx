import { BellOff, Mail, Send } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchInvoiceEmails, fetchReminderSettings, sendInvoiceEmail, updateInvoiceDelivery } from '../../lib/invoicesApi'
import type { InvoiceEmailResponse, InvoiceResponse, ReminderSettingsResponse } from '../../lib/types'

function when(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { day: 'numeric', month: 'short', year: 'numeric' })
}

function historyLine(e: InvoiceEmailResponse) {
  const what = e.kind === 'Invoice' ? 'Invoice emailed' : `Reminder (${e.reminderDay} day${e.reminderDay === 1 ? '' : 's'} after due)`
  if (e.status === 'Skipped') return { text: `${what} skipped: a later reminder was due`, tone: 'text-slate-400' }
  if (e.status === 'Failed') return { text: `${what} to ${e.toEmail} failed${e.error ? `: ${e.error}` : ''}`, tone: 'text-red-600' }
  return { text: `${what} to ${e.toEmail}`, tone: 'text-slate-600' }
}

/** Emailing the invoice to the customer, its reminder switch, and what has been sent so far. */
export function InvoiceDeliveryPanel({ invoice, onUpdated }: { invoice: InvoiceResponse; onUpdated: (invoice: InvoiceResponse) => void }) {
  const [email, setEmail] = useState(invoice.customerEmail ?? '')
  const [message, setMessage] = useState('')
  const [composing, setComposing] = useState(false)
  const [history, setHistory] = useState<InvoiceEmailResponse[]>([])
  const [settings, setSettings] = useState<ReminderSettingsResponse | null>(null)
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState<{ kind: 'ok' | 'error'; text: string } | null>(null)

  useEffect(() => {
    let cancelled = false
    fetchInvoiceEmails(invoice.id)
      .then((h) => !cancelled && setHistory(h))
      .catch(() => {})
    fetchReminderSettings()
      .then((s) => !cancelled && setSettings(s))
      .catch(() => {})
    return () => {
      cancelled = true
    }
  }, [invoice.id])

  const unpaid = invoice.status !== 'Paid'
  const emailChanged = email.trim().toLowerCase() !== (invoice.customerEmail ?? '')

  async function save(remindersPaused: boolean) {
    const before = invoice
    setBusy(true)
    setNotice(null)
    // Show the change at once; put it back if the server says no.
    onUpdated({ ...invoice, remindersPaused })
    try {
      onUpdated(await updateInvoiceDelivery(invoice.id, email.trim() || null, remindersPaused))
      setNotice({ kind: 'ok', text: 'Saved.' })
    } catch (err) {
      onUpdated(before)
      setNotice({ kind: 'error', text: err instanceof Error ? err.message : 'Failed to save' })
    } finally {
      setBusy(false)
    }
  }

  async function send() {
    setBusy(true)
    setNotice(null)
    try {
      const sent = await sendInvoiceEmail(invoice.id, email.trim() || undefined, message.trim() || undefined)
      setHistory((h) => [sent, ...h])
      onUpdated({ ...invoice, customerEmail: sent.toEmail })
      setComposing(false)
      setMessage('')
      setNotice({ kind: 'ok', text: `Invoice emailed to ${sent.toEmail}.` })
    } catch (err) {
      setNotice({ kind: 'error', text: err instanceof Error ? err.message : 'Failed to send the invoice' })
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="mt-6 rounded-xl border border-slate-200 p-4 print:hidden" data-testid="invoice-delivery">
      <h3 className="flex items-center gap-2 text-sm font-semibold text-slate-900">
        <Mail size={16} className="text-brand-primary" /> Email and reminders
      </h3>

      <div className="mt-3 flex flex-col gap-2 sm:flex-row">
        <label htmlFor="invoiceCustomerEmail" className="sr-only">
          Customer email
        </label>
        <input
          id="invoiceCustomerEmail"
          type="email"
          placeholder="Customer's email address"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
        />
        {emailChanged && !composing && (
          <button
            type="button"
            onClick={() => save(invoice.remindersPaused)}
            disabled={busy}
            className="shrink-0 rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-60"
          >
            Save email
          </button>
        )}
        {!composing && (
          <button
            type="button"
            onClick={() => setComposing(true)}
            disabled={!email.trim()}
            className="flex shrink-0 items-center justify-center gap-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
          >
            <Send size={15} /> {history.some((h) => h.kind === 'Invoice' && h.status === 'Sent') ? 'Email again' : 'Email invoice'}
          </button>
        )}
      </div>

      {composing && (
        <div className="mt-3 flex flex-col gap-2">
          <label htmlFor="invoiceMessage" className="text-xs font-medium text-slate-600">
            Message (optional)
          </label>
          <textarea
            id="invoiceMessage"
            rows={3}
            maxLength={2000}
            placeholder="Please find your invoice below. Thank you for your business."
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
          />
          <p className="text-xs text-slate-500">
            {invoice.customerName} gets the invoice in the email, with a link to view and download it. Replies come to you.
          </p>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={send}
              disabled={busy}
              className="flex items-center gap-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
            >
              <Send size={15} /> {busy ? 'Sending…' : `Send to ${email.trim()}`}
            </button>
            <button type="button" onClick={() => setComposing(false)} className="rounded-lg px-3 py-2 text-sm text-slate-500 hover:bg-slate-50">
              Cancel
            </button>
          </div>
        </div>
      )}

      {unpaid && (
        <label className="mt-4 flex cursor-pointer items-start gap-3">
          <input
            type="checkbox"
            checked={!invoice.remindersPaused}
            disabled={busy}
            onChange={(e) => save(!e.target.checked)}
            className="mt-0.5 h-4 w-4 accent-emerald-600"
            data-testid="remind-switch"
          />
          <span className="text-sm">
            <span className="font-medium text-slate-800">Send payment reminders for this invoice</span>
            <span className="block text-xs text-slate-500">
              {settings === null
                ? ''
                : !settings.enabled
                  ? 'Reminders are off for your business. '
                  : invoice.remindersPaused
                    ? 'Paused: this customer won’t be reminded.'
                    : `Reminders go out ${settings.days.join(', ')} days after the due date while it’s unpaid. `}
              {settings !== null && !settings.enabled && (
                <Link to="/settings" className="font-medium text-brand-primary hover:underline">
                  Turn them on in Settings
                </Link>
              )}
              {settings?.enabled && !invoice.customerEmail && ' Add an email address so they can be sent.'}
            </span>
          </span>
          {invoice.remindersPaused && <BellOff size={15} className="mt-0.5 text-slate-400" />}
        </label>
      )}

      {notice && (
        <p className={`mt-3 text-sm ${notice.kind === 'ok' ? 'text-emerald-700' : 'text-red-600'}`} role="status">
          {notice.text}
        </p>
      )}

      {history.length > 0 && (
        <ul className="mt-4 flex flex-col gap-1 border-t border-slate-100 pt-3 text-xs" data-testid="email-history">
          {history.map((e) => {
            const line = historyLine(e)
            return (
              <li key={e.id} className={`flex justify-between gap-3 ${line.tone}`}>
                <span>{line.text}</span>
                <span className="shrink-0 text-slate-400">{when(e.atUtc)}</span>
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
