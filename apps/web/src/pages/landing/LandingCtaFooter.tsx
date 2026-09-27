import { ArrowRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import logoLight from '../../assets/flowiq-logo-v2-light.png'
import { Logo } from '../../components/Logo'
import { Reveal } from '../../components/Reveal'

export function LandingCtaFooter() {
  return (
    <footer className="relative overflow-hidden bg-brand-navy">
      <div className="pointer-events-none absolute inset-0 overflow-hidden">
        <div className="absolute -left-24 bottom-0 h-72 w-72 animate-blob rounded-full bg-emerald-500/10 blur-3xl" />
        <div
          className="absolute right-0 top-0 h-64 w-64 animate-blob rounded-full bg-teal-500/10 blur-3xl"
          style={{ animationDelay: '-5s' }}
        />
      </div>

      <Reveal className="relative mx-auto flex max-w-7xl flex-col items-center justify-between gap-8 px-6 py-16 lg:flex-row lg:text-left">
        <div className="flex flex-col items-center lg:items-start">
          <Logo src={logoLight} />
          <p className="mt-2 text-sm text-slate-400">Smarter Finance. Bigger Dreams.</p>
        </div>

        <div className="text-center lg:text-left">
          <h2 className="text-2xl font-bold text-white sm:text-3xl">Ready to take control of your cash flow?</h2>
          <p className="mt-2 text-slate-400">Get started with FinFlow in minutes — no credit card required.</p>
        </div>

        <Link
          to="/register"
          target="_blank"
          rel="noopener noreferrer"
          className="group flex shrink-0 items-center gap-2 rounded-full bg-gradient-to-r from-emerald-500 to-teal-500 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-emerald-500/20 transition-all hover:shadow-xl hover:shadow-emerald-500/30 active:scale-95"
        >
          Start Your Free Trial
          <ArrowRight size={16} className="transition-transform group-hover:translate-x-1" />
        </Link>
      </Reveal>

      <div className="relative border-t border-white/10 px-6 py-6 text-center text-xs text-slate-500">
        © {new Date().getFullYear()} FinFlow. All rights reserved.
      </div>
    </footer>
  )
}
