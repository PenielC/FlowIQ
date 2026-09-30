import { Building2, Mail, User } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { PasswordInput } from '../../components/PasswordInput'
import { useAuth } from '../../lib/AuthContext'
import { AuthLayout } from './AuthLayout'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [companyName, setCompanyName] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await register({ companyName, firstName, lastName, email, password })
      // One quick question about personal withdrawals, so the first forecast they see is a believable one.
      navigate('/setup/forecast')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthLayout mode="register">
      <h1 className="text-center text-xl font-bold text-slate-900">Create your account</h1>
      <p className="mt-1 text-center text-sm text-slate-500">Start predicting your cash flow with FinFlow.</p>

      <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4">
        <div>
          <label htmlFor="companyName" className="mb-1 block text-sm font-medium text-slate-700">
            Company name
          </label>
          <div className="relative">
            <Building2 size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              id="companyName"
              required
              value={companyName}
              onChange={(e) => setCompanyName(e.target.value)}
              className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm transition-colors focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-primary/10"
            />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label htmlFor="firstName" className="mb-1 block text-sm font-medium text-slate-700">
              First name
            </label>
            <div className="relative">
              <User size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                id="firstName"
                required
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm transition-colors focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-primary/10"
              />
            </div>
          </div>
          <div>
            <label htmlFor="lastName" className="mb-1 block text-sm font-medium text-slate-700">
              Last name
            </label>
            <input
              id="lastName"
              required
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm transition-colors focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-primary/10"
            />
          </div>
        </div>
        <div>
          <label htmlFor="email" className="mb-1 block text-sm font-medium text-slate-700">
            Email Address
          </label>
          <div className="relative">
            <Mail size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              id="email"
              type="email"
              required
              placeholder="you@company.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm transition-colors focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-primary/10"
            />
          </div>
        </div>
        <div>
          <label htmlFor="password" className="mb-1 block text-sm font-medium text-slate-700">
            Password
          </label>
          <PasswordInput id="password" value={password} onChange={setPassword} minLength={8} placeholder="Create a strong password" />
          <p className="mt-1 text-xs text-slate-400">At least 8 characters.</p>
        </div>

        {error && <p className="text-sm text-red-500">{error}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="mt-2 rounded-lg bg-brand-primary px-4 py-2.5 text-sm font-semibold text-white transition-all hover:bg-emerald-700 hover:shadow-md active:scale-[0.98] disabled:opacity-60 disabled:active:scale-100"
        >
          {isSubmitting ? 'Creating account…' : 'Create Account'}
        </button>
      </form>
    </AuthLayout>
  )
}
