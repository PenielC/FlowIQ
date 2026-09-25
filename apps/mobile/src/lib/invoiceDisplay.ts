import { colors } from '@/constants/colors'

const STATUS_LABELS: Record<string, string> = {
  Draft: 'Draft',
  Sent: 'Pending',
  Paid: 'Paid',
  Overdue: 'Overdue',
}

const STATUS_COLORS: Record<string, { bg: string; text: string }> = {
  Draft: { bg: colors.border, text: colors.textSecondary },
  Sent: { bg: '#FBBF2433', text: colors.warning },
  Paid: { bg: colors.positive + '33', text: colors.positive },
  Overdue: { bg: colors.negative + '33', text: colors.negative },
}

export function invoiceStatusLabel(status: string) {
  return STATUS_LABELS[status] ?? status
}

export function invoiceStatusColors(status: string) {
  return STATUS_COLORS[status] ?? { bg: colors.border, text: colors.textSecondary }
}
