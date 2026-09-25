import type { LucideIcon } from 'lucide-react'
import { ArrowDownRight, ArrowUpRight } from 'lucide-react'

type StatCardProps = {
  icon: LucideIcon
  iconBg: string
  iconColor: string
  label: string
  value: string
  delta?: { value: string; positive: boolean }
  sublabel?: string
}

export function StatCard({ icon: Icon, iconBg, iconColor, label, value, delta, sublabel }: StatCardProps) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <span className={`inline-flex h-10 w-10 items-center justify-center rounded-xl ${iconBg}`}>
        <Icon size={20} className={iconColor} />
      </span>
      <p className="mt-4 text-sm text-slate-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-slate-900">{value}</p>
      {delta && (
        <p className={`mt-1 flex items-center gap-1 text-xs font-medium ${delta.positive ? 'text-emerald-600' : 'text-red-500'}`}>
          {delta.positive ? <ArrowUpRight size={14} /> : <ArrowDownRight size={14} />}
          {delta.value} vs last month
        </p>
      )}
      {sublabel && <p className="mt-1 text-xs text-slate-500">{sublabel}</p>}
    </div>
  )
}
