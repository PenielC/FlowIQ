import { ChevronLeft, ChevronRight } from 'lucide-react'

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

export function MonthSelector({
  year,
  month,
  onChange,
}: {
  year: number
  month: number
  onChange: (year: number, month: number) => void
}) {
  function goToPreviousMonth() {
    if (month === 1) {
      onChange(year - 1, 12)
    } else {
      onChange(year, month - 1)
    }
  }

  function goToNextMonth() {
    if (month === 12) {
      onChange(year + 1, 1)
    } else {
      onChange(year, month + 1)
    }
  }

  return (
    <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-2 py-1.5 shadow-sm">
      <button type="button" onClick={goToPreviousMonth} className="rounded-md p-1 text-slate-500 hover:bg-slate-50">
        <ChevronLeft size={16} />
      </button>
      <span className="min-w-[9rem] text-center text-sm font-medium text-slate-700">
        {MONTH_NAMES[month - 1]} {year}
      </span>
      <button type="button" onClick={goToNextMonth} className="rounded-md p-1 text-slate-500 hover:bg-slate-50">
        <ChevronRight size={16} />
      </button>
    </div>
  )
}
