import { colors } from '@/constants/colors'

const CATEGORY_LABELS: Record<string, string> = {
  Sales: 'Sales',
  OperatingExpense: 'Operating Expense',
  RentAndLease: 'Rent & Lease',
  Payroll: 'Payroll',
  Utilities: 'Utilities',
  Other: 'Other',
  OwnerDrawings: 'Owner Drawings',
  OwnerContribution: 'Owner Contribution',
}

const CATEGORY_COLORS: Record<string, string> = {
  Sales: colors.blue,
  OperatingExpense: '#F43F5E',
  RentAndLease: '#EC4899',
  Payroll: colors.purple,
  Utilities: colors.teal,
  Other: colors.textMuted,
  OwnerDrawings: '#D97706',
  OwnerContribution: '#0284C7',
}

export function categoryLabel(category: string) {
  return CATEGORY_LABELS[category] ?? category
}

export function categoryColor(category: string) {
  return CATEGORY_COLORS[category] ?? colors.textMuted
}

export function formatCurrency(amount: number, currencyCode: string) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: currencyCode }).format(amount)
}

export function currencySymbol(currencyCode: string) {
  const parts = new Intl.NumberFormat(undefined, { style: 'currency', currency: currencyCode }).formatToParts(0)
  return parts.find((p) => p.type === 'currency')?.value ?? currencyCode
}

export function formatDate(isoDate: string) {
  return new Date(isoDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}
