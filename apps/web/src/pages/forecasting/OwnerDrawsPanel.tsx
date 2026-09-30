import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useAuth } from '../../lib/AuthContext'
import { formatCurrency } from '../../lib/categoryDisplay'
import { deleteOwnerDraw } from '../../lib/forecastApi'
import type { DrawFrequency, OwnerDrawResponse } from '../../lib/types'
import { OwnerDrawModal } from './OwnerDrawModal'
import { DRAW_PRESETS, describeSchedule } from './ownerDrawDisplay'

type Editing = { draw?: OwnerDrawResponse; preset?: { name: string; frequency: DrawFrequency; months?: number[] } }

/** The owner's planned personal withdrawals, with quick-add buttons for the common ones. */
export function OwnerDrawsPanel({
  draws,
  isLoading,
  onChanged,
}: {
  draws: OwnerDrawResponse[]
  isLoading: boolean
  onChanged: () => void
}) {
  const { user } = useAuth()
  const companyCurrency = user?.companyCurrency ?? 'USD'
  const [editing, setEditing] = useState<Editing | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function handleDelete(draw: OwnerDrawResponse) {
    if (!window.confirm(`Remove "${draw.name}" from your forecast?`)) return
    setError(null)
    try {
      await deleteOwnerDraw(draw.id)
      onChanged()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete')
    }
  }

  const existingNames = new Set(draws.map((d) => d.name.toLowerCase()))
  const presets = DRAW_PRESETS.filter((p) => !existingNames.has(p.name.toLowerCase()))

  return (
    <div className="flex flex-col gap-3">
      {isLoading ? (
        <p className="text-sm text-slate-400">Loading…</p>
      ) : draws.length === 0 ? (
        <p className="text-sm text-slate-500">No planned withdrawals yet.</p>
      ) : (
        <ul className="divide-y divide-slate-100 rounded-xl border border-slate-200">
          {draws.map((draw) => (
            <li key={draw.id} className="flex items-center justify-between gap-3 px-4 py-3">
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold text-slate-900">{draw.name}</p>
                <p className="text-xs text-slate-500">{describeSchedule(draw)}</p>
              </div>
              <div className="flex shrink-0 items-center gap-3">
                <div className="text-right">
                  <p className="text-sm font-semibold text-slate-900">{formatCurrency(draw.amount, draw.currency)}</p>
                  {draw.currency !== companyCurrency && (
                    <p className="text-xs text-slate-400">≈ {formatCurrency(draw.amountInReportingCurrency, companyCurrency)}</p>
                  )}
                </div>
                <button
                  type="button"
                  onClick={() => setEditing({ draw })}
                  aria-label={`Edit ${draw.name}`}
                  className="text-slate-400 hover:text-slate-700"
                >
                  <Pencil size={16} />
                </button>
                <button
                  type="button"
                  onClick={() => handleDelete(draw)}
                  aria-label={`Delete ${draw.name}`}
                  className="text-slate-400 hover:text-red-600"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      {error && <p className="text-sm text-red-500">{error}</p>}

      <div className="flex flex-wrap gap-2">
        {presets.map((preset) => (
          <button
            key={preset.name}
            type="button"
            onClick={() => setEditing({ preset })}
            className="flex items-center gap-1.5 rounded-full border border-slate-200 px-3 py-1.5 text-sm text-slate-700 hover:border-brand-primary hover:text-brand-primary"
          >
            <Plus size={14} /> {preset.name}
          </button>
        ))}
        <button
          type="button"
          onClick={() => setEditing({})}
          className="flex items-center gap-1.5 rounded-full border border-dashed border-slate-300 px-3 py-1.5 text-sm text-slate-600 hover:border-brand-primary hover:text-brand-primary"
        >
          <Plus size={14} /> Something else
        </button>
      </div>

      {editing && (
        <OwnerDrawModal
          draw={editing.draw}
          preset={editing.preset}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            onChanged()
          }}
        />
      )}
    </div>
  )
}
