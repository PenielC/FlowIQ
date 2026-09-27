import { ArrowRight, Clock, Sparkles, TrendingUp } from 'lucide-react'
import { Link } from 'react-router-dom'
import showcaseDashboard from '../../assets/showcase-dashboard.png'
import showcaseMobile from '../../assets/showcase-mobile.png'

const bullets = [
  { icon: Sparkles, label: 'AI Powered', sublabel: 'Insights' },
  { icon: Clock, label: 'Save Time', sublabel: 'Automate Finance' },
  { icon: TrendingUp, label: 'Better Cash Flow', sublabel: 'More Growth' },
]

export function LandingHero() {
  return (
    <section id="top" className="relative overflow-hidden bg-brand-navy">
      <div className="pointer-events-none absolute inset-0 overflow-hidden">
        <div className="absolute -left-24 top-0 h-96 w-96 animate-blob rounded-full bg-emerald-500/20 blur-3xl" />
        <div
          className="absolute -right-10 top-1/4 h-80 w-80 animate-blob rounded-full bg-teal-500/20 blur-3xl"
          style={{ animationDelay: '-4s' }}
        />
        <div
          className="absolute bottom-0 left-1/3 h-72 w-72 animate-blob rounded-full bg-emerald-500/10 blur-3xl"
          style={{ animationDelay: '-8s' }}
        />
      </div>

      <div className="relative mx-auto grid max-w-7xl grid-cols-1 items-center gap-16 px-6 py-20 lg:grid-cols-2 lg:py-28">
        <div className="animate-fade-in-up">
          <span className="text-xs font-bold uppercase tracking-widest text-emerald-400">
            AI Cash Flow Intelligence Platform
          </span>
          <h1 className="mt-4 text-4xl font-bold leading-tight text-white sm:text-5xl">
            Smarter Cash Flow.
            <br />
            <span className="bg-gradient-to-r from-emerald-400 to-teal-300 bg-clip-text text-transparent">
              Stronger Businesses.
            </span>
          </h1>
          <p className="mt-5 max-w-lg text-lg text-slate-300">
            FinFlow helps African SMEs predict cash flow, automate financial categorisation, and improve collection of
            outstanding invoices through AI.
          </p>
          <div className="mt-8 flex flex-wrap items-center gap-4">
            <Link
              to="/register"
              target="_blank"
              rel="noopener noreferrer"
              className="group flex items-center gap-2 rounded-full bg-gradient-to-r from-emerald-500 to-teal-500 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-emerald-500/20 transition-all hover:shadow-xl hover:shadow-emerald-500/30 active:scale-95"
            >
              Start Free Trial
              <ArrowRight size={16} className="transition-transform group-hover:translate-x-1" />
            </Link>
            <Link
              to="/login"
              target="_blank"
              rel="noopener noreferrer"
              className="rounded-full border border-white/20 px-6 py-3 text-sm font-semibold text-white transition-colors hover:bg-white/5"
            >
              Sign In
            </Link>
          </div>

          <div className="mt-12 flex flex-wrap gap-8">
            {bullets.map((bullet) => (
              <div key={bullet.label} className="flex items-center gap-2.5">
                <bullet.icon size={18} className="text-emerald-400" />
                <div className="leading-tight">
                  <p className="text-sm font-semibold text-white">{bullet.label}</p>
                  <p className="text-xs text-slate-400">{bullet.sublabel}</p>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="animate-fade-in-up relative" style={{ animationDelay: '150ms' }}>
          {/* Laptop frame */}
          <div className="relative mx-auto max-w-xl rounded-t-xl border-[6px] border-slate-800 bg-slate-800 shadow-2xl">
            <div className="overflow-hidden rounded-t-md bg-white">
              <img src={showcaseDashboard} alt="FinFlow dashboard" className="h-64 w-full object-cover object-top sm:h-80" />
            </div>
          </div>
          <div className="relative mx-auto h-3 max-w-2xl rounded-b-xl bg-gradient-to-b from-slate-700 to-slate-800 shadow-xl" />
          <div className="mx-auto h-1.5 w-24 rounded-b-lg bg-slate-900" />

          {/* Phone frame, overlapping bottom-right */}
          <div className="absolute -bottom-6 -right-2 w-32 rounded-[1.5rem] border-[5px] border-slate-800 bg-slate-800 shadow-2xl sm:-right-6 sm:w-40">
            <div className="relative overflow-hidden rounded-[1.1rem] bg-white">
              <div className="absolute left-1/2 top-1 z-10 h-2.5 w-10 -translate-x-1/2 rounded-full bg-slate-800" />
              <img src={showcaseMobile} alt="FinFlow mobile app" className="h-56 w-full object-cover object-top sm:h-72" />
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
