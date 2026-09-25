import { Calendar, ChevronDown } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'

export interface DateRangeValue {
  startDateUtc: string
  endDateUtc: string
  label: string
}

function toIsoDate(d: Date) {
  return d.toISOString().slice(0, 10)
}

function formatLabel(start: Date, end: Date) {
  const fmt = (d: Date) => d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' })
  return `${fmt(start)} – ${fmt(end)}`
}

function rangeFromDates(start: Date, end: Date, label?: string): DateRangeValue {
  return { startDateUtc: toIsoDate(start), endDateUtc: toIsoDate(end), label: label ?? formatLabel(start, end) }
}

function startOfMonthUtc(d: Date) {
  return new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), 1))
}

function endOfMonthUtc(d: Date) {
  return new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth() + 1, 0))
}

export function getThisMonthRange(): DateRangeValue {
  const now = new Date()
  return { ...rangeFromDates(startOfMonthUtc(now), endOfMonthUtc(now)), label: 'This Month' }
}

const PRESETS: { label: string; compute: () => DateRangeValue }[] = [
  { label: 'This Month', compute: getThisMonthRange },
  {
    label: 'Last Month',
    compute: () => {
      const now = new Date()
      const lastMonth = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 1, 1))
      return { ...rangeFromDates(startOfMonthUtc(lastMonth), endOfMonthUtc(lastMonth)), label: 'Last Month' }
    },
  },
  {
    label: 'Last 30 Days',
    compute: () => {
      const now = new Date()
      const start = new Date(now)
      start.setUTCDate(start.getUTCDate() - 29)
      return { ...rangeFromDates(start, now), label: 'Last 30 Days' }
    },
  },
  {
    label: 'Last 90 Days',
    compute: () => {
      const now = new Date()
      const start = new Date(now)
      start.setUTCDate(start.getUTCDate() - 89)
      return { ...rangeFromDates(start, now), label: 'Last 90 Days' }
    },
  },
  {
    label: 'This Year',
    compute: () => {
      const now = new Date()
      return {
        ...rangeFromDates(new Date(Date.UTC(now.getUTCFullYear(), 0, 1)), new Date(Date.UTC(now.getUTCFullYear(), 11, 31))),
        label: 'This Year',
      }
    },
  },
]

export function DashboardHeader({ value, onChange }: { value: DateRangeValue; onChange: (value: DateRangeValue) => void }) {
  const [isOpen, setIsOpen] = useState(false)
  const [showCustom, setShowCustom] = useState(false)
  const [customStart, setCustomStart] = useState(value.startDateUtc)
  const [customEnd, setCustomEnd] = useState(value.endDateUtc)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false)
        setShowCustom(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  function selectPreset(preset: (typeof PRESETS)[number]) {
    onChange(preset.compute())
    setIsOpen(false)
    setShowCustom(false)
  }

  function applyCustomRange() {
    if (!customStart || !customEnd) return
    onChange(rangeFromDates(new Date(customStart), new Date(customEnd)))
    setIsOpen(false)
    setShowCustom(false)
  }

  return (
    <div className="mb-6 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Dashboard</h1>
        <p className="text-sm text-slate-500">Here's what's happening with your business today.</p>
      </div>
      <div ref={containerRef} className="relative">
        <button
          type="button"
          onClick={() => setIsOpen((o) => !o)}
          className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm text-slate-700 shadow-sm hover:bg-slate-50"
        >
          <Calendar size={16} className="text-slate-400" />
          {value.label}
          <ChevronDown size={16} className="text-slate-400" />
        </button>

        {isOpen && (
          <div className="absolute right-0 z-10 mt-1 w-56 rounded-lg border border-slate-200 bg-white py-1 shadow-lg">
            {PRESETS.map((preset) => (
              <button
                key={preset.label}
                type="button"
                onClick={() => selectPreset(preset)}
                className={`block w-full px-3 py-2 text-left text-sm hover:bg-slate-50 ${
                  value.label === preset.label ? 'font-semibold text-brand-primary' : 'text-slate-700'
                }`}
              >
                {preset.label}
              </button>
            ))}
            <button
              type="button"
              onClick={() => setShowCustom((s) => !s)}
              className={`block w-full px-3 py-2 text-left text-sm hover:bg-slate-50 ${
                showCustom ? 'font-semibold text-brand-primary' : 'text-slate-700'
              }`}
            >
              Custom Range
            </button>
            {showCustom && (
              <div className="flex flex-col gap-2 border-t border-slate-100 px-3 py-2">
                <input
                  type="date"
                  value={customStart}
                  onChange={(e) => setCustomStart(e.target.value)}
                  className="rounded-lg border border-slate-200 px-2 py-1 text-sm focus:border-brand-primary focus:outline-none"
                />
                <input
                  type="date"
                  value={customEnd}
                  onChange={(e) => setCustomEnd(e.target.value)}
                  className="rounded-lg border border-slate-200 px-2 py-1 text-sm focus:border-brand-primary focus:outline-none"
                />
                <button
                  type="button"
                  onClick={applyCustomRange}
                  className="rounded-lg bg-brand-primary px-3 py-1.5 text-sm font-semibold text-white hover:bg-emerald-700"
                >
                  Apply
                </button>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
