import { Sparkles, X } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { WHATS_NEW_CHANGED, dismissProductUpdate, fetchWhatsNew, type ProductUpdate } from '../../lib/whatsNewApi'

/** Up to two "New in FinFlow" cards on the dashboard. Following the link or closing one hides it for good. */
export function FeatureCards() {
  const navigate = useNavigate()
  const [cards, setCards] = useState<ProductUpdate[]>([])

  const load = useCallback(() => {
    fetchWhatsNew()
      .then((d) => setCards(d.dashboardCards))
      .catch(() => setCards([]))
  }, [])

  useEffect(() => {
    load()
    window.addEventListener(WHATS_NEW_CHANGED, load)
    return () => window.removeEventListener(WHATS_NEW_CHANGED, load)
  }, [load])

  function dismiss(id: string) {
    setCards((c) => c.filter((x) => x.id !== id))
    return dismissProductUpdate(id).catch(() => {})
  }

  // Save the dismissal before leaving, so the card doesn't come back if the next page is a full reload.
  async function follow(card: ProductUpdate) {
    await dismiss(card.id)
    if (card.linkUrl!.startsWith('/')) navigate(card.linkUrl!)
    else window.open(card.linkUrl!, '_blank', 'noreferrer')
  }

  if (cards.length === 0) return null
  return (
    <div className={`grid grid-cols-1 gap-4 ${cards.length > 1 ? 'lg:grid-cols-2' : ''}`}>
      {cards.map((card) => (
        <div
          key={card.id}
          className="flex gap-4 rounded-2xl border border-emerald-200 bg-gradient-to-br from-emerald-50 to-white p-5"
          data-testid="feature-card"
        >
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-emerald-100 text-emerald-700">
            <Sparkles size={18} />
          </span>
          <div className="min-w-0 flex-1">
            <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">New in FinFlow</p>
            <p className="mt-0.5 text-sm font-semibold text-slate-900">{card.title}</p>
            <p className="mt-1 text-sm text-slate-600">{card.summary}</p>
            {card.linkUrl && card.linkLabel && (
              <button
                type="button"
                onClick={() => follow(card)}
                className="mt-3 inline-flex rounded-lg bg-brand-primary px-3.5 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
              >
                {card.linkLabel}
              </button>
            )}
          </div>
          <button type="button" onClick={() => dismiss(card.id)} aria-label={`Dismiss ${card.title}`} className="self-start text-slate-400 hover:text-slate-600">
            <X size={16} />
          </button>
        </div>
      ))}
    </div>
  )
}
