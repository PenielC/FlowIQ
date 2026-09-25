import { Check, CheckCircle2, Sparkles } from 'lucide-react'
import { Reveal } from '../../components/Reveal'

const categoryBreakdown = [
  { label: 'Sales & Income', count: 142, color: 'bg-blue-500' },
  { label: 'Suppliers', count: 56, color: 'bg-amber-500' },
  { label: 'Operations', count: 32, color: 'bg-red-500' },
  { label: 'Marketing', count: 12, color: 'bg-purple-500' },
  { label: 'Other', count: 3, color: 'bg-slate-400' },
]

const checklist = ['Transaction categorisation', 'Cash flow forecasting', 'Invoice risk prediction']

export function InsightsShowcaseSection() {
  return (
    <section className="bg-emerald-50/40 py-24">
      <div className="mx-auto grid max-w-7xl grid-cols-1 items-center gap-16 px-6 lg:grid-cols-2">
        <Reveal>
          <div className="relative mx-auto max-w-md">
            <div className="absolute -inset-6 -z-10 rounded-full bg-emerald-200/40 blur-3xl" />
            <div className="rounded-2xl border border-slate-100 bg-white p-5 shadow-xl">
              <div className="mb-4 flex items-center gap-2">
                <Sparkles size={16} className="text-emerald-500" />
                <span className="text-sm font-semibold text-slate-900">Transaction Categories</span>
              </div>
              <ul className="flex flex-col gap-3">
                {categoryBreakdown.map((category) => (
                  <li key={category.label} className="flex items-center justify-between text-sm">
                    <span className="flex items-center gap-2 text-slate-600">
                      <span className={`h-2.5 w-2.5 rounded-full ${category.color}`} />
                      {category.label}
                    </span>
                    <span className="font-semibold text-slate-900">{category.count}</span>
                  </li>
                ))}
              </ul>
            </div>

            <div className="absolute -top-5 left-1/2 flex w-max -translate-x-1/2 items-center gap-2 whitespace-nowrap rounded-full border border-emerald-100 bg-white px-4 py-2 text-xs font-semibold text-slate-700 shadow-lg">
              <CheckCircle2 size={14} className="text-emerald-500" />
              AI has categorised 245 transactions
            </div>
          </div>
        </Reveal>

        <Reveal delayMs={100}>
          <span className="text-xs font-bold uppercase tracking-widest text-emerald-600">Powered by Advanced AI</span>
          <h2 className="mt-3 text-3xl font-bold leading-tight text-slate-900 sm:text-4xl">
            Turn Your Financial Data into Actionable Insights
          </h2>
          <p className="mt-4 text-slate-500">
            Our AI engine categorises transactions, predicts cash flow, identifies invoice risks and gives you the
            insights you need to grow — not just track.
          </p>
          <ul className="mt-6 flex flex-col gap-3">
            {checklist.map((item) => (
              <li key={item} className="flex items-center gap-2.5 text-sm font-medium text-slate-700">
                <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-emerald-100">
                  <Check size={12} className="text-emerald-600" />
                </span>
                {item}
              </li>
            ))}
          </ul>
        </Reveal>
      </div>
    </section>
  )
}
