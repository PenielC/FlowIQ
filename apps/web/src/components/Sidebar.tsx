import {
  Bookmark,
  FileText,
  HelpCircle,
  LayoutDashboard,
  LineChart,
  ListChecks,
  Receipt,
  Settings,
  Users,
} from 'lucide-react'
import { ChevronDown } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '../lib/AuthContext'
import { Logo } from './Logo'

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/transactions', label: 'Transactions', icon: ListChecks },
  { to: '/invoices', label: 'Invoices', icon: FileText },
  { to: '/forecasting', label: 'Forecasting', icon: LineChart },
  { to: '/customers', label: 'Customers', icon: Users },
  { to: '/subscriptions', label: 'Subscriptions', icon: Bookmark },
  { to: '/reports', label: 'Reports', icon: Receipt },
  { to: '/settings', label: 'Settings', icon: Settings },
]

export function Sidebar() {
  const { user } = useAuth()

  return (
    <aside className="flex w-64 shrink-0 flex-col justify-between bg-gradient-to-b from-slate-900 to-brand-navy px-4 py-6">
      <div>
        <Logo className="mb-8 ml-2" />
        <nav className="flex flex-col gap-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-brand-primary text-white'
                    : 'text-slate-400 hover:bg-white/5 hover:text-white'
                }`
              }
            >
              <item.icon size={18} strokeWidth={2} />
              {item.label}
            </NavLink>
          ))}
        </nav>
      </div>

      <div>
        <button
          type="button"
          className="mb-3 flex w-full items-center gap-3 rounded-xl bg-white/5 p-3 text-left transition-colors hover:bg-white/10"
        >
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-primary/20">
            <Bookmark size={16} className="text-brand-primary" />
          </span>
          <span className="min-w-0 flex-1">
            <span className="block truncate text-sm font-semibold text-white">{user?.companyName}</span>
            <span className="block text-xs text-slate-400">Main Company</span>
          </span>
          <ChevronDown size={16} className="text-slate-400" />
        </button>
        <a
          href="mailto:support@flowiq.com"
          className="flex items-center gap-2 px-1 text-xs text-slate-400 hover:text-white"
        >
          <HelpCircle size={14} />
          Need help? support@flowiq.com
        </a>
      </div>
    </aside>
  )
}
