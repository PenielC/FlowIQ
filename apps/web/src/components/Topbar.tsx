import { Bell, ChevronDown, LogOut, Search } from 'lucide-react'
import { useState } from 'react'
import { useAuth } from '../lib/AuthContext'

export function Topbar() {
  const { user, logout } = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)

  const initials = user ? `${user.firstName[0] ?? ''}${user.lastName[0] ?? ''}`.toUpperCase() : ''

  return (
    <header className="flex items-center justify-between border-b border-slate-200 bg-white px-8 py-4">
      <label className="flex w-full max-w-md items-center gap-2 rounded-lg bg-slate-100 px-3 py-2">
        <Search size={16} className="text-slate-400" />
        <input
          type="search"
          placeholder="Search transactions, customers, or invoices..."
          className="w-full bg-transparent text-sm text-slate-700 placeholder:text-slate-400 focus:outline-none"
        />
      </label>

      <div className="flex items-center gap-5">
        <button
          type="button"
          aria-label="Notifications"
          className="relative text-slate-400 hover:text-slate-600"
        >
          <Bell size={20} />
          <span className="absolute -right-0.5 -top-0.5 h-2 w-2 rounded-full bg-red-500" />
        </button>

        <div className="relative">
          <button type="button" className="flex items-center gap-2" onClick={() => setMenuOpen((v) => !v)}>
            <span className="flex h-9 w-9 items-center justify-center rounded-full bg-brand-primary text-sm font-semibold text-white">
              {initials}
            </span>
            <span className="text-left">
              <span className="block text-sm font-semibold text-slate-900">
                {user ? `${user.firstName} ${user.lastName}` : ''}
              </span>
              <span className="block text-xs text-slate-500">{user?.role}</span>
            </span>
            <ChevronDown size={16} className="text-slate-400" />
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
