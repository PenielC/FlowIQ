import { useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import logoDark from '../../assets/flowiq-logo-v2-dark.png'
import { Logo } from '../../components/Logo'
import { useAuth } from '../../lib/AuthContext'

export function AcceptInvitationPage() {
  const { acceptInvitation } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await acceptInvitation({ token, firstName, lastName, password })
      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to accept invitation')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
        <p className="text-sm text-slate-500">This invitation link is missing a token.</p>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 flex justify-center">
          <Logo variant="dark" src={logoDark} />
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
          <h1 className="text-xl font-bold text-slate-900">You've been invited</h1>
          <p className="mt-1 text-sm text-slate-500">Set up your account to join the team.</p>

          <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="firstName" className="mb-1 block text-sm font-medium text-slate-700">
                  First name
                </label>
                <input
                  id="firstName"
                  required
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
                />
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
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
                />
              </div>
            </div>

            <div>
              <label htmlFor="password" className="mb-1 block text-sm font-medium text-slate-700">
                Password
              </label>
              <input
                id="password"
                type="password"
                required
                minLength={8}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none"
              />
              <p className="mt-1 text-xs text-slate-400">At least 8 characters.</p>
            </div>

            {error && <p className="text-sm text-red-500">{error}</p>}

            <button
              type="submit"
              disabled={isSubmitting}
              className="mt-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
            >
              {isSubmitting ? 'Joining…' : 'Join the team'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
