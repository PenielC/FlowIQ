import type { DrawFrequency, OwnerDrawResponse } from '../../lib/types'

export const MONTH_NAMES = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export const FREQUENCY_LABELS: Record<DrawFrequency, string> = {
  Monthly: 'Every month',
  SelectedMonths: 'In chosen months (e.g. each school term)',
  Weekly: 'Every week',
  Once: 'Just once',
}

/** Quick starting points: the withdrawals owners mention most. */
export const DRAW_PRESETS: { name: string; frequency: DrawFrequency; months?: number[] }[] = [
  { name: 'School fees', frequency: 'SelectedMonths', months: [1, 5, 9] },
  { name: 'Home rent', frequency: 'Monthly' },
  { name: 'Household & family', frequency: 'Monthly' },
]

function ordinal(n: number) {
  const suffix = n % 10 === 1 && n !== 11 ? 'st' : n % 10 === 2 && n !== 12 ? 'nd' : n % 10 === 3 && n !== 13 ? 'rd' : 'th'
  return `${n}${suffix}`
}

/** "Every month on the 5th", "Jan, May, Sep on the 10th", "Every Friday", "Once, on 3 Oct 2026". */
export function describeSchedule(draw: Pick<OwnerDrawResponse, 'frequency' | 'nextDateUtc' | 'months'>) {
  const date = new Date(draw.nextDateUtc)
  const day = date.getUTCDate()
  switch (draw.frequency) {
    case 'Monthly':
      return `Every month on the ${ordinal(day)}`
    case 'SelectedMonths':
      return `${draw.months.map((m) => MONTH_NAMES[m - 1]).join(', ')} on the ${ordinal(day)}`
    case 'Weekly':
      return `Every ${date.toLocaleDateString('en-US', { weekday: 'long', timeZone: 'UTC' })}`
    case 'Once':
      return `Once, on ${date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' })}`
  }
}

/** yyyy-mm-dd of the 1st of next month, a sensible default next date. */
export function firstOfNextMonth() {
  const now = new Date()
  const d = new Date(Date.UTC(now.getFullYear(), now.getMonth() + 1, 1))
  return d.toISOString().slice(0, 10)
}
