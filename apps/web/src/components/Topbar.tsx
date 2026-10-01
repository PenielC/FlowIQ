import { ChevronDown, LogOut, Menu, Search } from 'lucide-react'
import { useState } from 'react'
import { useAuth } from '../lib/AuthContext'
import { WhatsNewButton } from './WhatsNewButton'

export function Topbar({ onMenuClick }: { onMenuClick: () => void }) {
  const { user, logout } = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)

  const initials = user ? `${user.firstName[0] ?? ''}${user.lastName[0] ?? ''}`.toUpperCase() : ''

  return (
    <header className="flex items-center justify-between gap-3 border-b border-slate-200 bg-white px-4 py-3 sm:px-6 lg:px-8 lg:py-4">
      <button
        type="button"
        onClick={onMenuClick}
        aria-label="Open menu"
        className="shrink-0 rounded-lg p-2 text-slate-600 hover:bg-slate-100 lg:hidden"
      >
        <Menu size={22} />
      </button>

      <label className="flex min-w-0 flex-1 items-center gap-2 rounded-lg bg-slate-100 px-3 py-2 md:max-w-md">
        <Search size={16} className="shrink-0 text-slate-400" />
        <input
          type="search"
          placeholder="Search transactions, customers, or invoices..."
          className="w-full min-w-0 bg-transparent text-sm text-slate-700 placeholder:text-slate-400 focus:outline-none"
        />
      </label>

      <div className="flex shrink-0 items-center gap-3 sm:gap-5">
        <WhatsNewButton />

        <div className="relative">
          <button type="button" className="flex items-center gap-2" onClick={() => setMenuOpen((v) => !v)}>
            <span className="flex h-9 w-9 items-center justify-center rounded-full bg-brand-primary text-sm font-semibold text-white">
              {initials}
            </span>
            <span className="hidden text-left sm:block">
              <span className="block text-sm font-semibold text-slate-900">
                {user ? `${user.firstName} ${user.lastName}` : ''}
              </span>
              <span className="block text-xs text-slate-500">{user?.role}</span>
            </span>
            <ChevronDown size={16} className="hidden text-slate-400 sm:block" />
          </button>

          {menuOpen && (
            <div className="absolute right-0 mt-2 w-40 rounded-lg border border-slate-200 bg-white py-1 shadow-md">
              <button
                type="button"
                onClick={logout}
                className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-slate-700 hover:bg-slate-50"
              >
                <LogOut size={14} />
                Log out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
