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

export function invoiceNumber(id: string) {
  return `INV-${id.slice(0, 8).toUpperCase()}`
}

/** "3 days overdue", "Due today", "Due in 5 days"; nothing once it's paid. */
export function dueDateNote(invoice: { status: string; dueDateUtc: string }) {
  if (invoice.status === 'Paid') return null

  const daysUntilDue = Math.ceil((new Date(invoice.dueDateUtc).getTime() - Date.now()) / 86400000)
  if (daysUntilDue < 0) return { text: `${Math.abs(daysUntilDue)} day${Math.abs(daysUntilDue) === 1 ? '' : 's'} overdue`, tone: 'text-red-600' }
  if (daysUntilDue === 0) return { text: 'Due today', tone: 'text-amber-600' }
  return { text: `Due in ${daysUntilDue} day${daysUntilDue === 1 ? '' : 's'}`, tone: 'text-slate-500' }
}
