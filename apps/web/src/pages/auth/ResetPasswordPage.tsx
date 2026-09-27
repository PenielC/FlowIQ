import { useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import logoDark from '../../assets/flowiq-logo-v2-dark.png'
import { Logo } from '../../components/Logo'
import { PasswordInput } from '../../components/PasswordInput'
import { useAuth } from '../../lib/AuthContext'

export function ResetPasswordPage() {
  const { resetPassword } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    setIsSubmitting(true)
    try {
      await resetPassword({ token, newPassword })
      navigate('/login?reset=success')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reset password')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
        <p className="text-sm text-slate-500">This reset link is missing a token.</p>
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
          <h1 className="text-xl font-bold text-slate-900">Set a new password</h1>
          <p className="mt-1 text-sm text-slate-500">Choose a new password for your account.</p>

          <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4">
            <div>
              <label htmlFor="newPassword" className="mb-1 block text-sm font-medium text-slate-700">
                New Password
              </label>
              <PasswordInput id="newPassword" value={newPassword} onChange={setNewPassword} minLength={8} placeholder="At least 8 characters" />
            </div>

            <div>
              <label htmlFor="confirmPassword" className="mb-1 block text-sm font-medium text-slate-700">
                Confirm Password
              </label>
              <PasswordInput id="confirmPassword" value={confirmPassword} onChange={setConfirmPassword} minLength={8} placeholder="Re-enter password" />
            </div>

            {error && <p className="text-sm text-red-500">{error}</p>}

            <button
              type="submit"
              disabled={isSubmitting}
              className="mt-2 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
            >
              {isSubmitting ? 'Resetting…' : 'Reset Password'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
