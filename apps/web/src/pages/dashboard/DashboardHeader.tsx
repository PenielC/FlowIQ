import { Calendar, ChevronDown } from 'lucide-react'

export function DashboardHeader() {
  return (
    <div className="mb-6 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Dashboard</h1>
        <p className="text-sm text-slate-500">Here's what's happening with your business today.</p>
      </div>
      <button
        type="button"
        className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm text-slate-700 shadow-sm"
      >
        <Calendar size={16} className="text-slate-400" />
        Apr 1, 2025 – Apr 30, 2025
        <ChevronDown size={16} className="text-slate-400" />
      </button>
    </div>
  )
}
