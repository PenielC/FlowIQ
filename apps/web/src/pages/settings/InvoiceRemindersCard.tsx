import { BellRing } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../../lib/AuthContext'
import { fetchReminderSettings, runRemindersNow, saveReminderSettings } from '../../lib/invoicesApi'
import type { ReminderSettingsResponse } from '../../lib/types'

function parseDays(text: string): number[] | null {
  const parts = text.split(/[,\s]+/).filter(Boolean)
  const days = parts.map(Number)
  if (!days.length || days.length > 5 || days.some((d) => !Number.isInteger(d) || d < 1 || d > 365)) return null
  return [...new Set(days)].sort((a, b) => a - b)
}

/** Settings: whether unpaid invoices are followed up by email, and on which days after the due date. */
export function InvoiceRemindersCard() {
  const { user } = useAuth()
  const canEdit = user?.role === 'Owner' || user?.role === 'Admin'
  const [saved, setSaved] = useState<ReminderSettingsResponse | null>(null)
  const [enabled, setEnabled] = useState(false)
  const [daysText, setDaysText] = useState('1, 7, 14')
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState<{ kind: 'ok' | 'error'; text: string } | null>(null)

  useEffect(() => {
    let cancelled = false
    fetchReminderSettings()
      .then((s) => {
        if (cancelled) return
        setSaved(s)
        setEnabled(s.enabled)
        setDaysText(s.days.join(', '))
      })
      .catch(() => {})
    return () => {
      cancelled = true
    }
  }, [])

  const days = parseDays(daysText)
  const dirty = saved !== null && (enabled !== saved.enabled || (days !== null && days.join() !== saved.days.join()))

  async function save() {
    if (!days) return
    setBusy(true)
    setNotice(null)
    try {
      const result = await saveReminderSettings(enabled, days)
      setSaved(result)
      setDaysText(result.days.join(', '))
      setNotice({ kind: 'ok', text: result.enabled ? 'Reminders are on.' : 'Reminders are off.' })
    } catch (err) {
      setNotice({ kind: 'error', text: err instanceof Error ? err.message : 'Failed to save' })
    } finally {
      setBusy(false)
    }
  }

  async function runNow() {
    setBusy(true)
    setNotice(null)
    try {
      const r = await runRemindersNow()
      const parts = [`${r.sent} reminder${r.sent === 1 ? '' : 's'} sent`]
      if (r.failed) parts.push(`${r.failed} failed (we'll try again later)`)
      setNotice({ kind: r.failed ? 'error' : 'ok', text: `${parts.join(', ')}.` })
    } catch (err) {
      setNotice({ kind: 'error', text: err instanceof Error ? err.message : 'Failed to send reminders' })
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-sm" data-testid="reminders-card">
      <div className="border-b border-slate-100 px-5 py-4">
        <h2 className="flex items-center gap-2 text-base font-semibold text-slate-900">
          <BellRing size={17} className="text-brand-primary" /> Payment reminders
        </h2>
        <p className="mt-1 text-sm text-slate-500">
          Automatically email customers a friendly reminder, with the invoice and a link to view it, when an invoice is
          unpaid after its due date. You can switch reminders off for any single invoice.
        </p>
      </div>
      <div className="flex flex-col gap-4 px-5 py-4">
        <label className="flex cursor-pointer items-center gap-3">
          <input
            type="checkbox"
            checked={enabled}
            disabled={!canEdit || saved === null}
            onChange={(e) => {
              setEnabled(e.target.checked)
              setNotice(null)
            }}
            className="h-4 w-4 accent-emerald-600"
            data-testid="reminders-enabled"
          />
          <span className="text-sm font-medium text-slate-800">Follow up unpaid invoices by email</span>
        </label>
        <div>
          <label htmlFor="reminderDays" className="mb-1 block text-sm font-medium text-slate-700">
            Days after the due date
          </label>
          <input
            id="reminderDays"
            value={daysText}
            disabled={!canEdit}
            onChange={(e) => {
              setDaysText(e.target.value)
              setNotice(null)
            }}
            className="w-full max-w-xs rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none disabled:bg-slate-50"
          />
          <p className={`mt-1 text-xs ${days ? 'text-slate-500' : 'text-red-600'}`}>
            {days
              ? `Up to 5 days, e.g. 1, 7, 14. Each reminder is sent once; if an invoice has already passed several, only the latest goes out.`
              : 'Enter 1 to 5 whole numbers between 1 and 365, separated by commas.'}
          </p>
        </div>
        {canEdit ? (
          <div className="flex flex-wrap items-center gap-3">
            <button
              type="button"
              onClick={save}
              disabled={busy || !dirty || !days}
              className="rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
            >
              {busy ? 'Saving…' : 'Save'}
            </button>
            {saved?.enabled && (
              <button
                type="button"
                onClick={runNow}
                disabled={busy}
                className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-60"
              >
                Send due reminders now
              </button>
            )}
          </div>
        ) : (
          <p className="text-xs text-slate-500">Only an owner or admin can change this.</p>
        )}
        <p className="text-xs text-slate-400">Reminders go out each morning (from 08:00 Harare time). Paid invoices are never chased.</p>
        {notice && (
          <p className={`text-sm ${notice.kind === 'ok' ? 'text-emerald-700' : 'text-red-600'}`} role="status">
            {notice.text}
          </p>
        )}
      </div>
    </div>
  )
}
