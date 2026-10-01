import { ChevronLeft, ChevronRight, Pencil, Power } from 'lucide-react'
import { useEffect, useState } from 'react'
import { activateCompany, deactivateCompany, fetchAdminOverview } from '../../lib/adminApi'
import { formatDate } from '../../lib/categoryDisplay'
import { subscriptionStatusColor, subscriptionStatusLabel } from '../../lib/subscriptionStatusDisplay'
import type { AdminCompanyRowResponse, AdminOverviewResponse } from '../../lib/types'
import { AdminGrowthChart } from './AdminGrowthChart'
import { AdminReportPanel } from './AdminReportPanel'
import { CurrencyRepairCard } from './CurrencyRepairCard'
import { ProductUpdatesCard } from './ProductUpdatesCard'
import { EditCompanyModal } from './EditCompanyModal'
import { MonthSelector } from './MonthSelector'

const PAGE_SIZE = 20

const STATUS_DISPLAY_ORDER = ['Active', 'Trialing', 'PastDue', 'Unpaid', 'Canceled', 'Incomplete', 'NoSubscription']

function getCurrentUtcYearMonth() {
  const now = new Date()
  return { year: now.getUTCFullYear(), month: now.getUTCMonth() + 1 }
}

export function AdminPage() {
  const [page, setPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState<string | null>(null)
  const [{ year, month }, setYearMonth] = useState(getCurrentUtcYearMonth)
  const [reloadToken, setReloadToken] = useState(0)
  const [overview, setOverview] = useState<AdminOverviewResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [editingCompany, setEditingCompany] = useState<AdminCompanyRowResponse | null>(null)
  const [togglingId, setTogglingId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    fetchAdminOverview(page, PAGE_SIZE, statusFilter ?? undefined, year, month)
      .then((r) => !cancelled && setOverview(r))
      .catch(() => !cancelled && setOverview(null))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [page, statusFilter, year, month, reloadToken])

  function goToPage(next: number) {
    setIsLoading(true)
    setPage(next)
  }

  function handleMonthChange(nextYear: number, nextMonth: number) {
    setIsLoading(true)
    setPage(1)
    setYearMonth({ year: nextYear, month: nextMonth })
  }

  function handleStatusFilterChange(next: string) {
    setIsLoading(true)
    setPage(1)
    setStatusFilter(next === '' ? null : next)
  }

  function handleSaved() {
    setEditingCompany(null)
    setIsLoading(true)
    setReloadToken((t) => t + 1)
  }

  async function handleToggleActive(company: AdminCompanyRowResponse) {
    const isDeactivating = company.isActive
    if (isDeactivating && !window.confirm(`Deactivate ${company.name}? Its users will no longer be able to sign in.`)) {
      return
    }

    setTogglingId(company.id)
    try {
      if (isDeactivating) {
        await deactivateCompany(company.id)
      } else {
        await activateCompany(company.id)
      }
      setReloadToken((t) => t + 1)
    } finally {
      setTogglingId(null)
    }
  }

  const companies = overview?.companies

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Platform Admin — All Companies</h1>
          <p className="text-sm text-slate-500">Cross-company view of signups, subscriptions and usage.</p>
        </div>
        <MonthSelector year={year} month={month} onChange={handleMonthChange} />
      </div>

      <AdminReportPanel overview={overview} isLoading={isLoading} />

      <AdminGrowthChart trend={overview?.monthlyTrend ?? []} isLoading={isLoading} />

      <ProductUpdatesCard />

      <CurrencyRepairCard />

      <div className="flex items-center justify-between">
        <h2 className="text-sm font-semibold text-slate-700">Companies</h2>
        <select
          value={statusFilter ?? ''}
          onChange={(e) => handleStatusFilterChange(e.target.value)}
          className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-700 shadow-sm focus:border-brand-primary focus:outline-none"
        >
          <option value="">All Statuses</option>
          {STATUS_DISPLAY_ORDER.map((status) => (
            <option key={status} value={status}>
              {subscriptionStatusLabel(status)}
            </option>
          ))}
        </select>
      </div>

      <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="w-full whitespace-nowrap text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
              <th className="px-5 py-3 font-medium">Company</th>
              <th className="px-5 py-3 font-medium">Owner</th>
              <th className="px-5 py-3 font-medium">Created</th>
              <th className="px-5 py-3 font-medium">Subscription</th>
              <th className="px-5 py-3 font-medium">Plan</th>
              <th className="px-5 py-3 font-medium">Account</th>
              <th className="px-5 py-3 font-medium"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              <tr>
                <td colSpan={7} className="px-5 py-8 text-center text-slate-400">
                  Loading…
                </td>
              </tr>
            ) : !companies || companies.items.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-5 py-8 text-center text-slate-400">
                  No companies match this filter.
                </td>
              </tr>
            ) : (
              companies.items.map((company) => (
                <tr key={company.id}>
                  <td className="px-5 py-3 font-medium text-slate-900">{company.name}</td>
                  <td className="px-5 py-3 text-slate-500">
                    {company.ownerName ? (
                      <>
                        {company.ownerName}
                        <span className="block text-xs text-slate-400">{company.ownerEmail}</span>
                      </>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td className="px-5 py-3 text-slate-500">{formatDate(company.createdAtUtc)}</td>
                  <td className="px-5 py-3">
                    <span
                      className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${subscriptionStatusColor(company.subscriptionStatus)}`}
                    >
                      {subscriptionStatusLabel(company.subscriptionStatus)}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-slate-500">{company.planKey ?? '—'}</td>
                  <td className="px-5 py-3">
                    <span
                      className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${
                        company.isActive ? 'bg-emerald-50 text-emerald-600' : 'bg-red-50 text-red-600'
                      }`}
                    >
                      {company.isActive ? 'Active' : 'Deactivated'}
                    </span>
                  </td>
                  <td className="px-5 py-3">
                    <div className="flex items-center justify-end gap-3">
                      <button
                        type="button"
                        onClick={() => setEditingCompany(company)}
                        className="flex items-center gap-1 text-xs font-medium text-slate-500 hover:text-slate-700 hover:underline"
                      >
                        <Pencil size={14} />
                        Edit
                      </button>
                      <button
                        type="button"
                        disabled={togglingId === company.id}
                        onClick={() => handleToggleActive(company)}
                        className={`flex items-center gap-1 text-xs font-medium hover:underline disabled:opacity-50 ${
                          company.isActive ? 'text-red-500 hover:text-red-700' : 'text-emerald-600 hover:text-emerald-700'
                        }`}
                      >
                        <Power size={14} />
                        {company.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {companies && companies.totalCount > 0 && (
          <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3">
            <p className="text-xs text-slate-500">
              Page {companies.pageNumber} of {companies.totalPages} · {companies.totalCount} total
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={!companies.hasPreviousPage}
                onClick={() => goToPage(page - 1)}
                className="rounded-lg border border-slate-200 p-1.5 text-slate-500 disabled:opacity-40"
              >
                <ChevronLeft size={16} />
              </button>
              <button
                type="button"
                disabled={!companies.hasNextPage}
                onClick={() => goToPage(page + 1)}
                className="rounded-lg border border-slate-200 p-1.5 text-slate-500 disabled:opacity-40"
              >
                <ChevronRight size={16} />
              </button>
            </div>
          </div>
        )}
      </div>

      {editingCompany && (
        <EditCompanyModal company={editingCompany} onClose={() => setEditingCompany(null)} onSaved={handleSaved} />
      )}
    </div>
  )
}
