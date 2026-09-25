import { Landmark, Receipt, TrendingDown, TrendingUp } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../lib/AuthContext'
import { formatCurrency } from '../lib/categoryDisplay'
import { fetchForecast } from '../lib/forecastApi'
import { fetchAiInsights } from '../lib/insightsApi'
import { fetchInvoiceSummary } from '../lib/invoicesApi'
import { fetchDashboardSummary } from '../lib/transactionsApi'
import type { CashFlowPointResponse, DashboardSummaryResponse, InsightResponse, InvoiceSummaryResponse } from '../lib/types'
import { AiCtaBanner } from './dashboard/AiCtaBanner'
import { AiInsightsCard } from './dashboard/AiInsightsCard'
import { DashboardHeader } from './dashboard/DashboardHeader'
import { ForecastChart } from './dashboard/ForecastChart'
import { RecentTransactionsCard } from './dashboard/RecentTransactionsCard'
import { StatCard } from './dashboard/StatCard'
import { UpcomingInvoicesCard } from './dashboard/UpcomingInvoicesCard'

function formatDelta(percent: number | null): { value: string; positive: boolean } | undefined {
  if (percent === null) return undefined
  return { value: `${Math.abs(percent)}%`, positive: percent >= 0 }
}

export function DashboardPage() {
  const { user } = useAuth()
  const currency = user?.companyCurrency ?? 'USD'
  const [summary, setSummary] = useState<DashboardSummaryResponse | null>(null)
  const [invoiceSummary, setInvoiceSummary] = useState<InvoiceSummaryResponse | null>(null)
  const [forecastPoints, setForecastPoints] = useState<CashFlowPointResponse[]>([])
  const [insights, setInsights] = useState<InsightResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isInvoicesLoading, setIsInvoicesLoading] = useState(true)
  const [isForecastLoading, setIsForecastLoading] = useState(true)
  const [isInsightsLoading, setIsInsightsLoading] = useState(true)

  useEffect(() => {
    fetchDashboardSummary()
      .then(setSummary)
      .catch(() => setSummary(null))
      .finally(() => setIsLoading(false))

    fetchInvoiceSummary()
      .then(setInvoiceSummary)
      .catch(() => setInvoiceSummary(null))
      .finally(() => setIsInvoicesLoading(false))

    fetchForecast()
      .then((res) => setForecastPoints(res.points))
      .catch(() => setForecastPoints([]))
      .finally(() => setIsForecastLoading(false))

    fetchAiInsights()
      .then((res) => setInsights(res.insights))
      .catch(() => setInsights([]))
      .finally(() => setIsInsightsLoading(false))
  }, [])

  return (
    <div className="flex flex-col gap-6">
      <DashboardHeader />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          icon={Landmark}
          iconBg="bg-blue-100"
          iconColor="text-blue-600"
          label="Total Cash Balance"
          value={isLoading ? '…' : formatCurrency(summary?.cashBalance ?? 0, currency)}
        />
        <StatCard
          icon={TrendingUp}
          iconBg="bg-violet-100"
          iconColor="text-violet-600"
          label="Monthly Revenue"
          value={isLoading ? '…' : formatCurrency(summary?.monthlyRevenue ?? 0, currency)}
          delta={formatDelta(summary?.revenueChangePercent ?? null)}
        />
        <StatCard
          icon={TrendingDown}
          iconBg="bg-teal-100"
          iconColor="text-teal-600"
          label="Total Expenses"
          value={isLoading ? '…' : formatCurrency(summary?.monthlyExpenses ?? 0, currency)}
          delta={formatDelta(summary?.expensesChangePercent ?? null)}
        />
        <StatCard
          icon={Receipt}
          iconBg="bg-purple-100"
          iconColor="text-purple-600"
          label="Outstanding Invoices"
          value={isInvoicesLoading ? '…' : formatCurrency(invoiceSummary?.totalOutstanding ?? 0, currency)}
          sublabel={
            isInvoicesLoading
              ? undefined
              : `${invoiceSummary?.customerCount ?? 0} ${invoiceSummary?.customerCount === 1 ? 'customer' : 'customers'}`
          }
        />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <ForecastChart points={forecastPoints} isLoading={isForecastLoading} currency={currency} />
        </div>
        <AiInsightsCard insights={insights} isLoading={isInsightsLoading} />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <RecentTransactionsCard transactions={summary?.recentTransactions ?? []} isLoading={isLoading} />
        <UpcomingInvoicesCard invoices={invoiceSummary?.upcomingInvoices ?? []} isLoading={isInvoicesLoading} />
      </div>

      <AiCtaBanner />
    </div>
  )
}
