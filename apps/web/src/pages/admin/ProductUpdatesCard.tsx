import { Megaphone, Pencil, Plus, Trash2, X } from 'lucide-react'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { formatDate } from '../../lib/categoryDisplay'
import {
  deleteProductUpdate,
  fetchAllProductUpdates,
  saveProductUpdate,
  setProductUpdatePublished,
  type ProductUpdate,
  type ProductUpdateAudience,
} from '../../lib/whatsNewApi'

const field = 'w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none'

function EditModal({ update, onClose, onSaved }: { update: ProductUpdate | null; onClose: () => void; onSaved: () => void }) {
  const [title, setTitle] = useState(update?.title ?? '')
  const [summary, setSummary] = useState(update?.summary ?? '')
  const [linkUrl, setLinkUrl] = useState(update?.linkUrl ?? '')
  const [linkLabel, setLinkLabel] = useState(update?.linkLabel ?? '')
  const [audience, setAudience] = useState<ProductUpdateAudience>(update?.audience ?? 'Everyone')
  const [showOnDashboard, setShowOnDashboard] = useState(update?.showOnDashboard ?? false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await saveProductUpdate(update?.id ?? null, {
        title,
        summary,
        linkUrl: linkUrl.trim() || null,
        linkLabel: linkLabel.trim() || null,
        audience,
        showOnDashboard,
      })
      onSaved()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4">
      <form onSubmit={submit} className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl" aria-label="Edit update">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-bold text-slate-900">{update ? 'Edit update' : 'New update'}</h2>
          <button type="button" onClick={onClose} aria-label="Close" className="text-slate-400 hover:text-slate-600">
            <X size={20} />
          </button>
        </div>
        <div className="flex flex-col gap-3">
          <div>
            <label htmlFor="puTitle" className="mb-1 block text-sm font-medium text-slate-700">
              Title
            </label>
            <input id="puTitle" required maxLength={100} value={title} onChange={(e) => setTitle(e.target.value)} className={field} placeholder="Let FinFlow chase late payers" />
          </div>
          <div>
            <label htmlFor="puSummary" className="mb-1 block text-sm font-medium text-slate-700">
              What it does for the user (one or two sentences)
            </label>
            <textarea id="puSummary" required rows={3} maxLength={400} value={summary} onChange={(e) => setSummary(e.target.value)} className={field} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="puLink" className="mb-1 block text-sm font-medium text-slate-700">
                Link
              </label>
              <input id="puLink" value={linkUrl} onChange={(e) => setLinkUrl(e.target.value)} className={field} placeholder="/settings or https://…" />
            </div>
            <div>
              <label htmlFor="puLinkLabel" className="mb-1 block text-sm font-medium text-slate-700">
                Button text
              </label>
              <input id="puLinkLabel" maxLength={40} value={linkLabel} onChange={(e) => setLinkLabel(e.target.value)} className={field} placeholder="Turn on reminders" />
            </div>
          </div>
          <div>
            <label htmlFor="puAudience" className="mb-1 block text-sm font-medium text-slate-700">
              Who sees it
            </label>
            <select id="puAudience" value={audience} onChange={(e) => setAudience(e.target.value as ProductUpdateAudience)} className={field}>
              <option value="Everyone">Everyone</option>
              <option value="OwnersAndAdmins">Owners and admins only</option>
            </select>
          </div>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" checked={showOnDashboard} onChange={(e) => setShowOnDashboard(e.target.checked)} className="h-4 w-4 accent-emerald-600" />
            Also show as a card on the dashboard (for 90 days, until dismissed)
          </label>
          {error && <p className="text-sm text-red-600">{error}</p>}
          <button type="submit" disabled={busy} className="mt-1 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60">
            {busy ? 'Saving…' : update ? 'Save changes' : 'Save as draft'}
          </button>
        </div>
      </form>
    </div>
  )
}

/** Platform admin: write, publish and retire the entries users see under What's new. */
export function ProductUpdatesCard() {
  const [updates, setUpdates] = useState<ProductUpdate[]>([])
  const [editing, setEditing] = useState<ProductUpdate | 'new' | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(() => {
    fetchAllProductUpdates()
      .then(setUpdates)
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load'))
  }, [])

  useEffect(load, [load])

  async function togglePublished(u: ProductUpdate) {
    setError(null)
    try {
      await setProductUpdatePublished(u.id, !u.publishedAtUtc)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update')
    }
  }

  async function remove(u: ProductUpdate) {
    if (!window.confirm(`Delete "${u.title}"? Users will no longer see it.`)) return
    await deleteProductUpdate(u.id).catch(() => {})
    load()
  }

  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-sm" data-testid="product-updates-card">
      <div className="flex items-start justify-between gap-4 border-b border-slate-100 p-5">
        <div>
          <h2 className="flex items-center gap-2 text-base font-semibold text-slate-900">
            <Megaphone size={17} className="text-brand-primary" /> What&apos;s new
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            Announce features to every user: a dot on the bell, an entry in the What&apos;s new panel and, if you choose, a dashboard card. New entries are drafts until you publish them.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setEditing('new')}
          className="flex shrink-0 items-center gap-1.5 rounded-lg bg-brand-primary px-3 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
        >
          <Plus size={15} /> New update
        </button>
      </div>
      {error && <p className="px-5 pt-3 text-sm text-red-600">{error}</p>}
      <ul className="divide-y divide-slate-100">
        {updates.map((u) => (
          <li key={u.id} className="flex flex-col gap-2 px-5 py-3 sm:flex-row sm:items-center" data-testid="product-update-row">
            <div className="min-w-0 flex-1">
              <p className="text-sm font-semibold text-slate-900">
                {u.title}{' '}
                <span className={`ml-1 rounded-full px-2 py-0.5 text-[11px] font-medium ${u.publishedAtUtc ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-600'}`}>
                  {u.publishedAtUtc ? `Published ${formatDate(u.publishedAtUtc)}` : 'Draft'}
                </span>
              </p>
              <p className="truncate text-xs text-slate-500">
                {u.audience === 'OwnersAndAdmins' ? 'Owners and admins' : 'Everyone'}
                {u.showOnDashboard ? ' · dashboard card' : ''} · {u.summary}
              </p>
            </div>
            <div className="flex shrink-0 items-center gap-1">
              <button type="button" onClick={() => togglePublished(u)} className="rounded-md px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-100">
                {u.publishedAtUtc ? 'Unpublish' : 'Publish'}
              </button>
              <button type="button" onClick={() => setEditing(u)} aria-label={`Edit ${u.title}`} className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700">
                <Pencil size={14} />
              </button>
              <button type="button" onClick={() => remove(u)} aria-label={`Delete ${u.title}`} className="rounded-md p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600">
                <Trash2 size={14} />
              </button>
            </div>
          </li>
        ))}
      </ul>
      {editing && (
        <EditModal
          update={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            load()
          }}
        />
      )}
    </div>
  )
}
