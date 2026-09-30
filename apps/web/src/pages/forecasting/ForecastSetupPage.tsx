import { CalendarClock } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { completeForecastSetup, fetchOwnerDraws } from '../../lib/forecastApi'
import type { OwnerDrawResponse } from '../../lib/types'
import { OwnerDrawsPanel } from './OwnerDrawsPanel'

/**
 * The one-question setup after sign-up: regular personal withdrawals from the business (school fees, home rent).
 * They are the most common reason a small business's forecast looks fine on Monday and wrong by Friday.
 */
export function ForecastSetupPage() {
  const navigate = useNavigate()
  const [answer, setAnswer] = useState<'unanswered' | 'yes'>('unanswered')
  const [draws, setDraws] = useState<OwnerDrawResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(() => {
    fetchOwnerDraws()
      .then((res) => {
        setDraws(res.draws)
        if (res.draws.length > 0) setAnswer('yes')
      })
      .catch(() => setDraws([]))
      .finally(() => setIsLoading(false))
  }, [])

  useEffect(load, [load])

  async function answerNo() {
    setIsSaving(true)
    setError(null)
    try {
      await completeForecastSetup()
      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save')
      setIsSaving(false)
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6 py-4">
      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
        <div className="mb-4 flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-100 text-emerald-700">
          <CalendarClock size={22} />
        </div>
        <p className="text-xs font-semibold uppercase tracking-wider text-brand-primary">Set up your forecast</p>
        <h1 className="mt-1 text-2xl font-bold text-slate-900">
          Do you take money from the business for personal or household costs?
        </h1>
        <p className="mt-2 text-sm text-slate-600">
          Things like school fees, home rent or family expenses, paid from the same till, bank account or mobile money
          wallet. Most owners do. Tell FinFlow when they're due and your forecast will show the dip on the right day,
          instead of looking fine on Monday and wrong by Friday.
        </p>

        {answer === 'unanswered' && !isLoading ? (
          <div className="mt-6 flex flex-wrap gap-3">
            <button
              type="button"
              onClick={() => setAnswer('yes')}
              className="rounded-lg bg-brand-primary px-5 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700"
            >
              Yes, let me add them
            </button>
            <button
              type="button"
              disabled={isSaving}
              onClick={answerNo}
              className="rounded-lg border border-slate-200 px-5 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
            >
              No, I don't
            </button>
          </div>
        ) : (
          answer === 'yes' && (
            <div className="mt-6 flex flex-col gap-5">
              <OwnerDrawsPanel draws={draws} isLoading={isLoading} onChanged={load} />
              <p className="text-xs text-slate-500">
                Rough amounts are fine. You can change these any time on the Forecasting page. When you take the money out,
                record it as <span className="font-medium text-slate-700">Owner Drawings</span> so it isn't counted as a
                business expense.
              </p>
              <div>
                <button
                  type="button"
                  onClick={() => navigate('/dashboard')}
                  disabled={draws.length === 0}
                  className="rounded-lg bg-brand-primary px-5 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
                >
                  Done, show my dashboard
                </button>
              </div>
            </div>
          )
        )}

        {error && <p className="mt-4 text-sm text-red-500">{error}</p>}
      </div>

      <button type="button" onClick={() => navigate('/dashboard')} className="self-center text-sm text-slate-500 hover:text-slate-700">
        Skip for now
      </button>
    </div>
  )
}
