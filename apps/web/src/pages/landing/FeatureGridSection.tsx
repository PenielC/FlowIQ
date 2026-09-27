import { ArrowRight, FileText, RefreshCcw, ShieldCheck, Smartphone, TrendingUp, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Reveal } from '../../components/Reveal'

const features = [
  {
    icon: RefreshCcw,
    title: 'AI Categorisation',
    description: 'Automatically categorise transactions with AI, saving you hours of manual work.',
  },
  {
    icon: TrendingUp,
    title: 'Cash Flow Forecasting',
    description: 'Predict your future cash flow and plan with confidence.',
  },
  {
    icon: FileText,
    title: 'Invoice Management',
    description: 'Create, send and track invoices. Get notified and improve collection rates.',
  },
  {
    icon: Users,
    title: 'Team Collaboration',
    description: 'Manage your team, set roles, and keep everyone aligned.',
  },
  {
    icon: ShieldCheck,
    title: 'Secure & Reliable',
    description: 'Bank-level security with JWT, role-based access, and full audit logging.',
  },
  {
    icon: Smartphone,
    title: 'Multi-Platform',
    description: 'Access on web and mobile, anytime, anywhere.',
  },
]

export function FeatureGridSection() {
  return (
    <section id="features" className="mx-auto max-w-7xl px-6 py-24">
      <div className="grid grid-cols-1 gap-12 lg:grid-cols-3">
        <Reveal className="lg:col-span-1">
          <span className="text-xs font-bold uppercase tracking-widest text-emerald-600">Built for African SMEs</span>
          <h2 className="mt-3 text-3xl font-bold leading-tight text-slate-900 sm:text-4xl">
            Everything You Need for Financial Control
          </h2>
          <p className="mt-4 text-slate-500">
            From bank transactions to invoice collection, FinFlow brings your business finances together with the
            power of AI — so you can make smarter decisions, faster.
          </p>
          <Link
            to="/register"
            target="_blank"
            rel="noopener noreferrer"
            className="group mt-6 inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-emerald-500 to-teal-500 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-emerald-500/20 transition-all hover:shadow-xl hover:shadow-emerald-500/30 active:scale-95"
          >
            Get Started Today
            <ArrowRight size={16} className="transition-transform group-hover:translate-x-1" />
          </Link>
        </Reveal>

        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:col-span-2">
          {features.map((feature, i) => (
            <Reveal key={feature.title} delayMs={i * 70}>
              <div className="group h-full rounded-2xl border border-slate-100 p-5 transition-colors hover:bg-emerald-50/50">
                <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-50 transition-colors group-hover:bg-emerald-100">
                  <feature.icon size={20} className="text-emerald-600" />
                </span>
                <h3 className="mt-4 text-base font-semibold text-slate-900">{feature.title}</h3>
                <p className="mt-1.5 text-sm text-slate-500">{feature.description}</p>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
