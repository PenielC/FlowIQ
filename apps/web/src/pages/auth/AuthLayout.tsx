import { Banknote, Coins, CreditCard, DollarSign, PiggyBank, Receipt, Sparkles, TrendingUp, Wallet } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import logoDark from '../../assets/flowiq-logo-v2-dark.png'
import logoLight from '../../assets/flowiq-logo-v2-light.png'
import { Logo } from '../../components/Logo'

const features = [
  {
    icon: Sparkles,
    title: 'AI-Powered Insights',
    description: 'Smarter decisions with real-time cash flow predictions.',
  },
  {
    icon: TrendingUp,
    title: 'Automated Categorisation',
    description: 'Save time and stay organised.',
  },
  {
    icon: Receipt,
    title: 'Faster Collections',
    description: 'Reduce late payments and improve your cash flow.',
  },
]

const floatingCards = [
  { label: 'Cash Flow Forecast', value: '$24,580', delta: '↗ +12%', positive: true },
  { label: 'Outstanding Invoices', value: '$8,420', delta: '3 overdue', positive: false },
  { label: 'Invoice Collection Rate', value: '92%', delta: null, positive: true },
]

const floatingCoins = [
  // Edges — gentle in-place bob
  { icon: DollarSign, left: '5%', bottom: '10px', size: 24, delay: '0s', duration: '5s', motion: 'float' as const },
  { icon: Coins, left: '18%', bottom: '28px', size: 20, delay: '-1.5s', duration: '6s', motion: 'float' as const },
  { icon: Wallet, left: '78%', bottom: '26px', size: 22, delay: '-4.5s', duration: '5.8s', motion: 'float' as const },
  { icon: Coins, left: '92%', bottom: '6px', size: 20, delay: '-2.3s', duration: '6.5s', motion: 'float' as const },
  // Middle — rise all the way from the bottom to just below the Sign In button
  { icon: Banknote, left: '32%', bottom: '0px', size: 26, delay: '0s', duration: '8s', motion: 'rise' as const },
  { icon: PiggyBank, left: '48%', bottom: '0px', size: 22, delay: '-2.6s', duration: '9s', motion: 'rise' as const },
  { icon: DollarSign, left: '64%', bottom: '0px', size: 20, delay: '-5.3s', duration: '7.5s', motion: 'rise' as const },
  // Rising trail up the side margins
  { icon: Banknote, left: '3%', bottom: '90px', size: 20, delay: '-3.5s', duration: '6s', motion: 'float' as const },
  { icon: CreditCard, left: '94%', bottom: '110px', size: 22, delay: '-1s', duration: '5.3s', motion: 'float' as const },
  { icon: Coins, left: '2%', bottom: '190px', size: 16, delay: '-5s', duration: '6.8s', motion: 'float' as const },
  { icon: DollarSign, left: '95%', bottom: '210px', size: 18, delay: '-2.8s', duration: '5.1s', motion: 'float' as const },
  { icon: PiggyBank, left: '4%', bottom: '300px', size: 20, delay: '-4.2s', duration: '6.3s', motion: 'float' as const },
]

export function AuthLayout({ mode, children }: { mode: 'login' | 'register'; children: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col lg:flex-row">
      <div className="relative flex w-full flex-col justify-between overflow-hidden bg-brand-navy p-8 sm:p-12 lg:w-1/2">
        <div className="pointer-events-none absolute inset-0 overflow-hidden">
          <div className="absolute -left-24 -top-24 h-96 w-96 animate-blob rounded-full bg-brand-primary/30 blur-3xl" />
          <div
            className="absolute right-0 top-1/3 h-80 w-80 animate-blob rounded-full bg-teal-500/20 blur-3xl"
            style={{ animationDelay: '-4s' }}
          />
          <div
            className="absolute bottom-0 left-1/3 h-72 w-72 animate-blob rounded-full bg-brand-purple/20 blur-3xl"
            style={{ animationDelay: '-8s' }}
          />
        </div>

        <Link to="/" className="relative z-10 w-fit">
          <Logo className="h-6" src={logoLight} />
        </Link>

        <div className="relative z-10 mt-8">
          <span className="text-xs font-bold uppercase tracking-widest text-emerald-400">
            AI Cash Flow Intelligence Platform
          </span>
          <h1 className="mt-3 max-w-md text-3xl font-bold leading-tight text-white sm:text-4xl">
            Smarter Cash Flow.
            <br />
            <span className="bg-gradient-to-r from-emerald-400 to-teal-300 bg-clip-text text-transparent">
              Stronger Businesses.
            </span>
          </h1>
          <p className="mt-4 max-w-md text-slate-300">
            Predict cash flow, automate financial categorisation, and improve collection of outstanding invoices —
            all powered by AI.
          </p>

          <ul className="mt-8 flex flex-col gap-4">
            {features.map((feature) => (
              <li key={feature.title} className="flex items-start gap-3">
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-white/10">
                  <feature.icon size={16} className="text-emerald-400" />
                </span>
                <div>
                  <p className="text-sm font-semibold text-white">{feature.title}</p>
                  <p className="text-sm text-slate-400">{feature.description}</p>
                </div>
              </li>
            ))}
          </ul>

          <div className="relative mt-10 hidden max-w-md xl:block">
            <div className="overflow-hidden rounded-2xl shadow-2xl">
              <img
                src="https://images.unsplash.com/photo-1758874384232-cfa79a5babf1?auto=format&fit=crop&w=800&q=80"
                alt="A small business owner working on her laptop"
                className="h-56 w-full object-cover"
              />
            </div>

            <div className="absolute -bottom-6 -left-4 w-44 rounded-xl bg-white p-3 shadow-xl">
              <p className="text-[11px] text-slate-500">{floatingCards[0].label}</p>
              <p className="text-lg font-bold text-slate-900">{floatingCards[0].value}</p>
              <p className="text-xs font-medium text-emerald-600">{floatingCards[0].delta}</p>
            </div>

            <div className="absolute -right-6 top-10 w-44 rounded-xl bg-white p-3 shadow-xl">
              <p className="text-[11px] text-slate-500">{floatingCards[1].label}</p>
              <p className="text-lg font-bold text-slate-900">{floatingCards[1].value}</p>
              <p className="text-xs font-medium text-red-500">{floatingCards[1].delta}</p>
            </div>
          </div>
        </div>

        <p className="relative z-10 mt-8 text-xs text-slate-500">© {new Date().getFullYear()} FinFlow</p>
      </div>

      <div className="relative flex w-full flex-1 items-start justify-center overflow-y-auto bg-slate-50 px-4 pb-10 pt-12 lg:bg-white">
        <div className="pointer-events-none absolute inset-x-0 bottom-0 h-[420px] overflow-hidden">
          {floatingCoins.map((coin, i) => (
            <coin.icon
              key={i}
              size={coin.size}
              className={`absolute text-brand-primary/40 ${coin.motion === 'rise' ? 'animate-rise' : 'animate-float'}`}
              style={{
                left: coin.left,
                bottom: coin.bottom,
                animationDelay: coin.delay,
                animationDuration: coin.duration,
                ...(coin.motion === 'rise' ? { '--rise-distance': '-390px' } : {}),
              }}
            />
          ))}
        </div>

        <div className="relative w-full max-w-sm">
          <div className="mb-8 flex justify-center">
            <Link to="/">
              <Logo variant="dark" src={logoDark} />
            </Link>
          </div>

          <div className="mb-6 flex rounded-full bg-slate-100 p-1">
            <Link
              to="/login"
              className={`flex-1 rounded-full py-2 text-center text-sm font-semibold transition-colors ${
                mode === 'login' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              Sign In
            </Link>
            <Link
              to="/register"
              className={`flex-1 rounded-full py-2 text-center text-sm font-semibold transition-colors ${
                mode === 'register' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              Create Account
            </Link>
          </div>

          {children}
        </div>
      </div>
    </div>
  )
}
