import { ArrowRight, Sparkles } from 'lucide-react'

function handleViewInsights() {
  const card = document.getElementById('ai-insights-card')
  if (!card) return
  card.scrollIntoView({ behavior: 'smooth', block: 'center' })
  card.classList.add('ring-2', 'ring-brand-primary', 'ring-offset-2')
  setTimeout(() => card.classList.remove('ring-2', 'ring-brand-primary', 'ring-offset-2'), 1500)
}

export function AiCtaBanner() {
  return (
    <div className="flex flex-col items-start justify-between gap-4 rounded-2xl bg-gradient-to-r from-brand-navy via-indigo-800 to-brand-purple p-6 sm:flex-row sm:items-center">
      <div className="flex items-center gap-4">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-white/10">
          <Sparkles size={20} className="text-white" />
        </span>
        <div>
          <p className="font-semibold text-white">Let AI do the heavy lifting</p>
          <p className="text-sm text-slate-300">Get smarter insights, better forecasts and actionable recommendations.</p>
        </div>
      </div>
      <button
        type="button"
        onClick={handleViewInsights}
        className="flex shrink-0 items-center gap-2 rounded-lg bg-white px-4 py-2 text-sm font-semibold text-brand-navy hover:bg-slate-100"
      >
        View Insights
        <ArrowRight size={16} />
      </button>
    </div>
  )
}
