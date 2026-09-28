import type { LucideIcon } from 'lucide-react'
import {
  Bookmark,
  FileText,
  HelpCircle,
  LayoutDashboard,
  LineChart,
  ListChecks,
  MessageSquarePlus,
  Receipt,
  Settings,
  Shield,
  Users,
} from 'lucide-react'
import { ChevronDown } from 'lucide-react'
import { useState } from 'react'
import { NavLink } from 'react-router-dom'
import logoLight from '../assets/flowiq-logo-v2-light.png'
import { useAuth } from '../lib/AuthContext'
import { FeedbackModal } from './FeedbackModal'
import { Logo } from './Logo'

type NavItem = { to: string; label: string; icon: LucideIcon; end?: boolean }

const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/transactions', label: 'Transactions', icon: ListChecks },
  { to: '/invoices', label: 'Invoices', icon: FileText },
  { to: '/forecasting', label: 'Forecasting', icon: LineChart },
  { to: '/customers', label: 'Customers', icon: Users },
  { to: '/subscriptions', label: 'Subscriptions', icon: Bookmark },
  { to: '/reports', label: 'Reports', icon: Receipt },
  { to: '/settings', label: 'Settings', icon: Settings },
]

const adminNavItem: NavItem = { to: '/admin', label: 'Admin', icon: Shield }

export function Sidebar() {
  const { user } = useAuth()
  const items = user?.isPlatformAdmin ? [...navItems, adminNavItem] : navItems
  const [showFeedbackModal, setShowFeedbackModal] = useState(false)

  return (
    <aside className="flex w-64 shrink-0 flex-col justify-between bg-gradient-to-b from-slate-900 to-brand-navy px-4 py-6">
      <div>
        <Logo src={logoLight} className="mb-8 ml-2" />
        <nav className="flex flex-col gap-1">
          {items.map((item) => (
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
        <button
          type="button"
          onClick={() => setShowFeedbackModal(true)}
          className="mb-2 flex w-full items-center gap-2 px-1 text-xs text-slate-400 hover:text-white"
        >
          <MessageSquarePlus size={14} />
          Send Feedback
        </button>
        <a
          href="mailto:support@flowiqfinance.com"
          className="flex items-center gap-2 px-1 text-xs text-slate-400 hover:text-white"
        >
          <HelpCircle size={14} />
          Need help? support@flowiqfinance.com
        </a>
      </div>

      {showFeedbackModal && <FeedbackModal onClose={() => setShowFeedbackModal(false)} />}
    </aside>
  )
}
