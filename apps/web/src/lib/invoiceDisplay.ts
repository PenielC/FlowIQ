const STATUS_LABELS: Record<string, string> = {
  Draft: 'Draft',
  Sent: 'Pending',
  Paid: 'Paid',
  Overdue: 'Overdue',
}

const STATUS_COLORS: Record<string, string> = {
  Draft: 'bg-slate-50 text-slate-500',
  Sent: 'bg-amber-50 text-amber-600',
  Paid: 'bg-emerald-50 text-emerald-600',
  Overdue: 'bg-red-50 text-red-600',
}

export function invoiceStatusLabel(status: string) {
  return STATUS_LABELS[status] ?? status
}

export function invoiceStatusColor(status: string) {
  return STATUS_COLORS[status] ?? 'bg-slate-50 text-slate-500'
}
