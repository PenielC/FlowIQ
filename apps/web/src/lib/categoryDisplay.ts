const CATEGORY_LABELS: Record<string, string> = {
  Sales: 'Sales',
  OperatingExpense: 'Operating Expense',
  RentAndLease: 'Rent & Lease',
  Payroll: 'Payroll',
  Utilities: 'Utilities',
  Other: 'Other',
}

const CATEGORY_COLORS: Record<string, string> = {
  Sales: 'bg-blue-100 text-blue-600',
  OperatingExpense: 'bg-rose-100 text-rose-600',
  RentAndLease: 'bg-pink-100 text-pink-600',
  Payroll: 'bg-violet-100 text-violet-600',
  Utilities: 'bg-teal-100 text-teal-600',
  Other: 'bg-slate-100 text-slate-600',
}

export function categoryLabel(category: string) {
  return CATEGORY_LABELS[category] ?? category
}

export function categoryColor(category: string) {
  return CATEGORY_COLORS[category] ?? 'bg-slate-100 text-slate-600'
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
