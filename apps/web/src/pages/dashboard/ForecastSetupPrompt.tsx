import { CalendarClock, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { completeForecastSetup, fetchOwnerDraws } from '../../lib/forecastApi'

/** Asks owners who haven't answered the forecast setup about regular personal withdrawals. Hidden once answered. */
export function ForecastSetupPrompt({ onAnswered }: { onAnswered?: () => void }) {
  const navigate = useNavigate()
  const [show, setShow] = useState(false)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    let cancelled = false
    fetchOwnerDraws()
      .then((res) => !cancelled && setShow(!res.setupCompleted))
      .catch(() => !cancelled && setShow(false))
    return () => {
      cancelled = true
    }
  }, [])

  async function answerNo() {
    setIsSaving(true)
    try {
      await completeForecastSetup()
      setShow(false)
      onAnswered?.()
    } finally {
      setIsSaving(false)
    }
  }

  if (!show) return null

  return (
    <div className="flex flex-col gap-4 rounded-2xl border border-amber-200 bg-amber-50 p-5 sm:flex-row sm:items-center">
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-amber-100 text-amber-700">
        <CalendarClock size={20} />
      </div>
      <div className="flex-1">
        <p className="text-sm font-semibold text-slate-900">Do you pay school fees or rent from the business?</p>
        <p className="text-sm text-slate-600">
          Add your regular personal withdrawals so your forecast shows the dips before they happen.
        </p>
      </div>
      <div className="flex shrink-0 flex-wrap items-center gap-2">
        <button
          type="button"
          onClick={() => navigate('/setup/forecast')}
          className="rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
        >
          Set up now
        </button>
        <button
          type="button"
          disabled={isSaving}
          onClick={answerNo}
          className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-60"
        >
          I don't take money out
        </button>
        <button type="button" onClick={() => setShow(false)} aria-label="Remind me later" className="p-1 text-slate-400 hover:text-slate-600">
          <X size={18} />
        </button>
      </div>
    </div>
  )
}
