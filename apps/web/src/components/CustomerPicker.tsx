import { useEffect, useRef, useState } from 'react'
import { fetchCustomers } from '../lib/customersApi'
import type { CustomerResponse } from '../lib/types'

export function CustomerPicker({
  id,
  value,
  onChange,
}: {
  id: string
  value: string
  onChange: (value: string) => void
}) {
  const [customers, setCustomers] = useState<CustomerResponse[]>([])
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    fetchCustomers(1, 100)
      .then((r) => setCustomers(r.items))
      .catch(() => {})
  }, [])

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const trimmed = value.trim().toLowerCase()
  const filtered = trimmed ? customers.filter((c) => c.name.toLowerCase().includes(trimmed)) : customers

  return (
    <div ref={containerRef} className="relative">
      <input
        id={id}
        required
        autoComplete="off"
        value={value}
        onChange={(e) => {
          onChange(e.target.value)
          setIsOpen(true)
        }}
        onFocus={() => setIsOpen(true)}
        placeholder="Type a name or pick a customer"
        className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
      />

      {isOpen && filtered.length > 0 && (
        <div className="absolute z-10 mt-1 max-h-48 w-full overflow-y-auto rounded-lg border border-slate-200 bg-white py-1 shadow-lg">
          {filtered.map((customer) => (
            <button
              key={customer.id}
              type="button"
              onClick={() => {
                onChange(customer.name)
                setIsOpen(false)
              }}
              className="block w-full px-3 py-2 text-left text-sm hover:bg-slate-50"
            >
              <p className="font-medium text-slate-900">{customer.name}</p>
              {customer.email && <p className="text-xs text-slate-400">{customer.email}</p>}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
