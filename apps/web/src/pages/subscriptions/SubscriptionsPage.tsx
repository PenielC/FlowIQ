import { Check, CreditCard, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useAuth } from '../../lib/AuthContext'
import { createBillingPortalSession, createCheckoutSession, fetchSubscriptionPlans, fetchSubscriptionStatus } from '../../lib/subscriptionsApi'
import type { SubscriptionPlanResponse, SubscriptionStatusResponse } from '../../lib/types'

const STATUS_LABELS: Record<string, string> = {
  Incomplete: 'Incomplete',
  Trialing: 'Trialing',
  Active: 'Active',
  PastDue: 'Past Due',
  Canceled: 'Canceled',
  Unpaid: 'Unpaid',
}

const STATUS_COLORS: Record<string, string> = {
  Incomplete: 'bg-slate-100 text-slate-600',
  Trialing: 'bg-blue-100 text-blue-600',
  Active: 'bg-emerald-100 text-emerald-600',
  PastDue: 'bg-amber-100 text-amber-600',
  Canceled: 'bg-red-100 text-red-600',
  Unpaid: 'bg-red-100 text-red-600',
}

export function SubscriptionsPage() {
  const { user } = useAuth()
  const isOwner = user?.role === 'Owner'
  const [searchParams] = useSearchParams()
  const checkoutResult = searchParams.get('checkout')

  const [plans, setPlans] = useState<SubscriptionPlanResponse[]>([])
  const [status, setStatus] = useState<SubscriptionStatusResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [pendingPlanKey, setPendingPlanKey] = useState<string | null>(null)
  const [isOpeningPortal, setIsOpeningPortal] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    Promise.all([fetchSubscriptionPlans(), fetchSubscriptionStatus()])
      .then(([p, s]) => {
        if (!cancelled) {
          setPlans(p)
          setStatus(s)
        }
      })
      .catch(() => !cancelled && setError('Failed to load subscription info.'))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [])

  async function handleSubscribe(planKey: string) {
    setError(null)
    setPendingPlanKey(planKey)
    try {
      const url = await createCheckoutSession(planKey)
      window.location.assign(url)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start checkout')
      setPendingPlanKey(null)
    }
  }

  async function handleManageBilling() {
    setError(null)
    setIsOpeningPortal(true)
    try {
      const url = await createBillingPortalSession()
      window.location.assign(url)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to open billing portal')
      setIsOpeningPortal(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Subscriptions</h1>
        <p className="text-sm text-slate-500">Manage your FlowIQ plan and billing.</p>
      </div>

      {checkoutResult === 'success' && (
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
          Checkout complete — your subscription will appear here once Stripe confirms it.
        </div>
      )}
      {checkoutResult === 'cancelled' && (
        <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">
          Checkout was cancelled — no changes were made.
        </div>
      )}
      {error && <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">{error}</div>}

      {!isLoading && status?.hasSubscription && (
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-base font-semibold text-slate-900">{status.planDisplayName} Plan</h2>
                <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_COLORS[status.status ?? ''] ?? 'bg-slate-100 text-slate-600'}`}>
                  {STATUS_LABELS[status.status ?? ''] ?? status.status}
                </span>
              </div>
              <p className="mt-1 text-sm text-slate-500">
                {status.status === 'Trialing' ? (
                  <>
                    Free trial
                    {status.currentPeriodEndUtc && ` · ends ${new Date(status.currentPeriodEndUtc).toLocaleDateString()}`}
                    {` · then $${status.monthlyPriceUsd?.toFixed(2)}/mo`}
                  </>
                ) : (
                  <>
                    ${status.monthlyPriceUsd?.toFixed(2)}/mo
                    {status.currentPeriodEndUtc && ` · renews ${new Date(status.currentPeriodEndUtc).toLocaleDateString()}`}
                  </>
                )}
              </p>
            </div>
            {isOwner && (
              <button
                type="button"
                onClick={handleManageBilling}
                disabled={isOpeningPortal}
                className="flex items-center gap-2 rounded-lg border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
              >
                <CreditCard size={16} />
                {isOpeningPortal ? 'Opening…' : 'Manage Billing'}
              </button>
            )}
          </div>
        </div>
      )}

      {!isLoading && !status?.hasSubscription && (
        <div className="mx-auto w-full max-w-sm">
          {plans.map((plan) => (
            <div key={plan.key} className="flex flex-col rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-lg font-bold text-slate-900">{plan.displayName}</h2>
              {plan.trialDays > 0 && (
                <p className="mt-1 text-sm font-semibold text-emerald-600">
                  Free for {plan.trialDays} days
                </p>
              )}
              <p className="mt-1 text-3xl font-bold text-slate-900">
                ${plan.monthlyPriceUsd.toFixed(0)}
                <span className="text-sm font-normal text-slate-500">/mo</span>
              </p>
              {plan.trialDays > 0 && <p className="mt-1 text-xs text-slate-400">after your free trial, cancel anytime</p>}
              <ul className="mt-4 flex flex-col gap-2 text-sm text-slate-600">
                {plan.features.map((feature) => (
                  <li key={feature} className="flex items-start gap-2">
                    <Check size={16} className="mt-0.5 shrink-0 text-emerald-500" />
                    {feature}
                  </li>
                ))}
              </ul>
              {isOwner ? (
                <button
                  type="button"
                  onClick={() => handleSubscribe(plan.key)}
                  disabled={pendingPlanKey !== null}
                  className="mt-6 rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
                >
                  {pendingPlanKey === plan.key
                    ? 'Redirecting…'
                    : plan.trialDays > 0
                      ? 'Start Free Trial'
                      : `Subscribe to ${plan.displayName}`}
                </button>
              ) : (
                <p className="mt-6 flex items-center gap-2 text-xs text-slate-400">
                  <X size={14} />
                  Only the account owner can manage billing
                </p>
              )}
            </div>
          ))}
        </div>
      )}

      {isLoading && <p className="text-sm text-slate-400">Loading…</p>}
    </div>
  )
}
