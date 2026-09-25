import { AlertTriangle, Sparkles, TrendingUp } from 'lucide-react'
import type { InsightResponse } from '../../lib/types'

const toneStyles: Record<string, { bg: string; color: string; icon: typeof TrendingUp }> = {
  Positive: { bg: 'bg-emerald-100', color: 'text-emerald-600', icon: TrendingUp },
  Warning: { bg: 'bg-amber-100', color: 'text-amber-600', icon: AlertTriangle },
  Caution: { bg: 'bg-orange-100', color: 'text-orange-600', icon: TrendingUp },
}

export function AiInsightsCard({ insights, isLoading }: { insights: InsightResponse[]; isLoading: boolean }) {
  return (
    <div id="ai-insights-card" className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition-shadow duration-700 ease-out">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="flex items-center gap-2 text-base font-semibold text-slate-900">
          <Sparkles size={16} className="text-brand-purple" />
          AI Insights
        </h2>
      </div>

      {isLoading ? (
        <p className="text-sm text-slate-400">Loading…</p>
      ) : insights.length === 0 ? (
        <p className="text-sm text-slate-400">Nothing to flag right now — you're all caught up.</p>
      ) : (
        <ul className="flex flex-col gap-4">
          {insights.map((insight) => {
            const style = toneStyles[insight.tone] ?? toneStyles.Positive
            return (
              <li key={insight.title} className="flex gap-3">
                <span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full ${style.bg}`}>
                  <style.icon size={16} className={style.color} />
                </span>
                <div>
                  <p className="text-sm font-medium text-slate-900">{insight.title}</p>
                  <p className="text-xs text-slate-500">{insight.description}</p>
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
