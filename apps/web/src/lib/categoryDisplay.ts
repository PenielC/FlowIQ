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

/** Short help shown when an owner category is picked, since these are the unfamiliar ones. */
export const CATEGORY_HINTS: Record<string, string> = {
  OwnerDrawings: 'Money you take out for personal or household costs, like school fees or home rent. Not a business expense.',
  OwnerContribution: 'Money you put into the business from your own pocket. Not revenue.',
}

const CATEGORY_COLORS: Record<string, string> = {
  Sales: 'bg-blue-100 text-blue-600',
  OperatingExpense: 'bg-rose-100 text-rose-600',
  RentAndLease: 'bg-pink-100 text-pink-600',
  Payroll: 'bg-violet-100 text-violet-600',
  Utilities: 'bg-teal-100 text-teal-600',
  Other: 'bg-slate-100 text-slate-600',
  OwnerDrawings: 'bg-amber-100 text-amber-700',
  OwnerContribution: 'bg-sky-100 text-sky-700',
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
