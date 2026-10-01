import { ArrowRight, Bell, X } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { formatDate } from '../lib/categoryDisplay'
import { WHATS_NEW_CHANGED, fetchWhatsNew, markWhatsNewSeen, type WhatsNew } from '../lib/whatsNewApi'

/** The bell in the top bar: a dot when something new has shipped, and a panel listing what's new. */
export function WhatsNewButton() {
  const [data, setData] = useState<WhatsNew | null>(null)
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  const load = useCallback(() => {
    fetchWhatsNew()
      .then(setData)
      .catch(() => {})
  }, [])

  useEffect(() => {
    load()
    window.addEventListener(WHATS_NEW_CHANGED, load)
    return () => window.removeEventListener(WHATS_NEW_CHANGED, load)
  }, [load])

  useEffect(() => {
    if (!open) return
    const close = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false)
        load()
      }
    }
    document.addEventListener('mousedown', close)
    return () => document.removeEventListener('mousedown', close)
  }, [open, load])

  function toggle() {
    const next = !open
    setOpen(next)
    if (!next) {
      load()
      return
    }
    // Opening the panel counts as reading it: the dot goes now, the "New" labels when it's closed.
    if (data && data.unreadCount > 0) {
      setData({ ...data, unreadCount: 0 })
      markWhatsNewSeen().catch(() => {})
    }
  }

  const unread = data?.unreadCount ?? 0
  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        onClick={toggle}
        aria-label={unread ? `What's new: ${unread} unread` : "What's new"}
        aria-expanded={open}
        className="relative text-slate-400 hover:text-slate-600"
        data-testid="whats-new-button"
      >
        <Bell size={20} />
        {unread > 0 && <span className="absolute -right-0.5 -top-0.5 h-2 w-2 rounded-full bg-red-500" data-testid="whats-new-dot" />}
      </button>

      {open && (
        <div
          className="absolute right-0 z-40 mt-3 w-[min(22rem,calc(100vw-2rem))] overflow-hidden rounded-xl border border-slate-200 bg-white shadow-lg"
          role="dialog"
          aria-label="What's new"
          data-testid="whats-new-panel"
        >
          <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3">
            <p className="text-sm font-semibold text-slate-900">What&apos;s new in FinFlow</p>
            <button type="button" onClick={toggle} aria-label="Close" className="text-slate-400 hover:text-slate-600">
              <X size={16} />
            </button>
          </div>
          <ul className="max-h-[70vh] divide-y divide-slate-100 overflow-y-auto">
            {(data?.updates ?? []).length === 0 && <li className="px-4 py-6 text-center text-sm text-slate-400">Nothing new yet.</li>}
            {(data?.updates ?? []).map((u) => (
              <li key={u.id} className="px-4 py-3">
                <div className="flex items-start justify-between gap-2">
                  <p className="text-sm font-semibold text-slate-900">{u.title}</p>
                  {u.isUnread && <span className="shrink-0 rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-semibold text-emerald-700">New</span>}
                </div>
                <p className="mt-1 text-sm text-slate-600">{u.summary}</p>
                <div className="mt-2 flex items-center justify-between">
                  {u.publishedAtUtc && <span className="text-xs text-slate-400">{formatDate(u.publishedAtUtc)}</span>}
                  {u.linkUrl && u.linkLabel && (
                    <UpdateLink href={u.linkUrl} onClick={() => setOpen(false)}>
                      {u.linkLabel} <ArrowRight size={13} />
                    </UpdateLink>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}

/** In-app paths navigate inside FinFlow; https links open in a new tab. */
export function UpdateLink({ href, onClick, children, className }: { href: string; onClick?: () => void; children: React.ReactNode; className?: string }) {
  const style = className ?? 'flex items-center gap-1 text-xs font-semibold text-brand-primary hover:underline'
  return href.startsWith('/') ? (
    <Link to={href} onClick={onClick} className={style}>
      {children}
    </Link>
  ) : (
    <a href={href} target="_blank" rel="noreferrer" onClick={onClick} className={style}>
      {children}
    </a>
  )
}
