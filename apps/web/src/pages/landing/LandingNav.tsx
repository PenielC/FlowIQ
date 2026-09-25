import { Link } from 'react-router-dom'
import { Logo } from '../../components/Logo'

export function LandingNav() {
  return (
    <header className="sticky top-0 z-50 border-b border-white/10 bg-brand-navy/80 backdrop-blur-md">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
        <Logo />

        <nav className="hidden items-center gap-8 md:flex">
          <a href="#top" className="flex flex-col items-center gap-1.5 text-sm font-medium text-white">
            Home
            <span className="h-0.5 w-4 rounded-full bg-emerald-400" />
          </a>
          <a href="#features" className="text-sm font-medium text-slate-300 transition-colors hover:text-white">
            Features
          </a>
        </nav>

        <div className="flex items-center gap-3">
          <Link
            to="/login"
            target="_blank"
            rel="noopener noreferrer"
            className="hidden rounded-lg px-4 py-2 text-sm font-medium text-slate-200 transition-colors hover:text-white sm:block"
          >
            Sign In
          </Link>
          <Link
            to="/register"
            target="_blank"
            rel="noopener noreferrer"
            className="rounded-full bg-gradient-to-r from-emerald-500 to-teal-500 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-emerald-500/20 transition-all hover:shadow-xl hover:shadow-emerald-500/30 active:scale-95"
          >
            Get Started Free
          </Link>
        </div>
      </div>
    </header>
  )
}
