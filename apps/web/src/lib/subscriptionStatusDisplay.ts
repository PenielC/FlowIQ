const STATUS_LABELS: Record<string, string> = {
  Active: 'Active',
  Trialing: 'Trialing',
  PastDue: 'Past Due',
  Canceled: 'Canceled',
  Unpaid: 'Unpaid',
  Incomplete: 'Incomplete',
  NoSubscription: 'No Subscription',
}

const STATUS_COLORS: Record<string, string> = {
  Active: 'bg-emerald-50 text-emerald-600',
  Trialing: 'bg-blue-50 text-blue-600',
  PastDue: 'bg-amber-50 text-amber-600',
  Canceled: 'bg-slate-100 text-slate-500',
  Unpaid: 'bg-red-50 text-red-600',
  Incomplete: 'bg-slate-100 text-slate-500',
  NoSubscription: 'bg-slate-50 text-slate-400',
}

export function subscriptionStatusLabel(status: string) {
  return STATUS_LABELS[status] ?? status
}

export function subscriptionStatusColor(status: string) {
  return STATUS_COLORS[status] ?? 'bg-slate-50 text-slate-500'
}
