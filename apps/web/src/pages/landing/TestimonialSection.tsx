import { Atom, Cloud, CreditCard, Database, Layers, Package } from 'lucide-react'
import { Reveal } from '../../components/Reveal'

const techStack = [
  { icon: Atom, label: 'React (Web & Mobile)', color: 'text-sky-500' },
  { icon: Layers, label: '.NET 10 (Clean Architecture)', color: 'text-violet-500' },
  { icon: Database, label: 'PostgreSQL (Neon)', color: 'text-emerald-500' },
  { icon: Package, label: 'Docker', color: 'text-blue-500' },
  { icon: CreditCard, label: 'Stripe Billing', color: 'text-indigo-500' },
  { icon: Cloud, label: 'Azure Ready', color: 'text-sky-600' },
]

export function TestimonialSection() {
  return (
    <section className="mx-auto max-w-7xl px-6 py-24">
      <div className="grid grid-cols-1 gap-12 lg:grid-cols-2">
        <Reveal className="relative">
          <div className="overflow-hidden rounded-2xl shadow-lg">
            <img
              src="https://images.unsplash.com/photo-1687422808384-c896d0efd4ab?auto=format&fit=crop&w=1000&q=80"
              alt="A small business owner"
              className="h-96 w-full object-cover"
            />
          </div>
          <div className="absolute -bottom-8 right-4 max-w-xs rounded-2xl border border-slate-100 bg-white p-5 shadow-xl sm:-right-8">
            <p className="text-sm text-slate-600">
              &ldquo;So many African SMEs are still running cash flow from memory and a notebook. We're building the
              tool we wish existed — one that shows you where your money's going before it's too late.&rdquo;
            </p>
            <p className="mt-3 text-sm font-semibold text-slate-900">— Peniel Chingombe</p>
            <p className="text-xs text-slate-400">Founder, FinFlow</p>
          </div>
        </Reveal>

        <Reveal delayMs={100} className="lg:pt-8">
          <span className="text-xs font-bold uppercase tracking-widest text-emerald-600">Built for Growth</span>
          <h2 className="mt-3 text-3xl font-bold leading-tight text-slate-900 sm:text-4xl">
            Modern Technology. Enterprise Grade Architecture.
          </h2>
          <p className="mt-4 text-slate-500">
            FinFlow is built with modern, scalable technology and best practices to ensure security, performance and
            reliability — today and tomorrow.
          </p>

          <ul className="mt-8 grid grid-cols-1 gap-4 sm:grid-cols-2">
            {techStack.map((tech) => (
              <li key={tech.label} className="flex items-center gap-2.5 text-sm font-medium text-slate-700">
                <tech.icon size={18} className={tech.color} />
                {tech.label}
              </li>
            ))}
          </ul>
        </Reveal>
      </div>
    </section>
  )
}
