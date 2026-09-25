import { Eye, EyeOff, Lock } from 'lucide-react'
import { useState } from 'react'

export function PasswordInput({
  id,
  value,
  onChange,
  minLength,
  placeholder,
}: {
  id: string
  value: string
  onChange: (value: string) => void
  minLength?: number
  placeholder?: string
}) {
  const [visible, setVisible] = useState(false)

  return (
    <div className="relative">
      <Lock size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
      <input
        id={id}
        type={visible ? 'text' : 'password'}
        required
        minLength={minLength}
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-9 text-sm transition-colors focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-primary/10"
      />
      <button
        type="button"
        onClick={() => setVisible((v) => !v)}
        className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
        tabIndex={-1}
        aria-label={visible ? 'Hide password' : 'Show password'}
      >
        {visible ? <EyeOff size={16} /> : <Eye size={16} />}
      </button>
    </div>
  )
}
