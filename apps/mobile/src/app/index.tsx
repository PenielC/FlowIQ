import { Logo } from '@/components/Logo'
import { Sparkline } from '@/components/Sparkline'
import { colors } from '@/constants/colors'
import { categoryColor, categoryLabel, formatCurrency, formatDate } from '@/lib/categoryDisplay'
import { useAuth } from '@/lib/AuthContext'
import { fetchForecast } from '@/lib/forecastApi'
import { fetchAiInsights } from '@/lib/insightsApi'
import { fetchInvoiceSummary } from '@/lib/invoicesApi'
import { fetchDashboardSummary } from '@/lib/transactionsApi'
import type { CashFlowPointResponse, DashboardSummaryResponse, InsightResponse, InvoiceSummaryResponse } from '@/lib/types'
import { ChevronRight, CreditCard, Menu, Sparkles, Wallet } from 'lucide-react-native'
import { useEffect, useState } from 'react'
import { ActivityIndicator, ScrollView, StyleSheet, Text, View } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'

function formatDelta(percent: number | null) {
  if (percent === null) return null
  const sign = percent >= 0 ? '↑' : '↓'
  return { text: `${sign} ${Math.abs(percent)}%`, positive: percent >= 0 }
}

export default function HomeScreen() {
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

  const revenueDelta = formatDelta(summary?.revenueChangePercent ?? null)
  const expensesDelta = formatDelta(summary?.expensesChangePercent ?? null)

  const lastActualBalance = [...forecastPoints].reverse().find((p) => p.actual !== null)?.actual ?? null
  const forecastedEndBalance = forecastPoints.length > 0 ? forecastPoints[forecastPoints.length - 1].forecast : null
  const forecastChangePercent =
    lastActualBalance !== null && lastActualBalance !== 0 && forecastedEndBalance !== null
      ? Math.round(((forecastedEndBalance - lastActualBalance) / Math.abs(lastActualBalance)) * 1000) / 10
      : null
  const forecastDelta = formatDelta(forecastChangePercent)

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
        <View style={styles.header}>
          <Logo height={28} />
          <Menu size={22} color={colors.textPrimary} />
        </View>

        <Text style={styles.greeting}>Good morning, {user?.firstName} 👋</Text>
        <Text style={styles.subtitle}>{"Here's your business snapshot for today."}</Text>

        <View style={styles.balanceCard}>
          <View style={styles.balanceIcon}>
            <Wallet size={18} color={colors.primary} />
          </View>
          <View style={styles.balanceInfo}>
            <Text style={styles.balanceLabel}>Cash Balance</Text>
            <Text style={styles.balanceValue}>{isLoading ? '…' : formatCurrency(summary?.cashBalance ?? 0, currency)}</Text>
          </View>
          <Sparkline />
        </View>

        <View style={styles.statGrid}>
          <View style={styles.statCard}>
            <Text style={styles.statLabel}>Revenue</Text>
            <Text style={styles.statValue}>{isLoading ? '…' : formatCurrency(summary?.monthlyRevenue ?? 0, currency)}</Text>
            {revenueDelta && (
              <Text style={[styles.statDelta, { color: revenueDelta.positive ? colors.positive : colors.negative }]}>
                {revenueDelta.text}
              </Text>
            )}
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statLabel}>Expenses</Text>
            <Text style={styles.statValue}>{isLoading ? '…' : formatCurrency(summary?.monthlyExpenses ?? 0, currency)}</Text>
            {expensesDelta && (
              <Text style={[styles.statDelta, { color: expensesDelta.positive ? colors.positive : colors.negative }]}>
                {expensesDelta.text}
              </Text>
            )}
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statLabel}>Outstanding</Text>
            <Text style={styles.statValue}>
              {isInvoicesLoading ? '…' : formatCurrency(invoiceSummary?.totalOutstanding ?? 0, currency)}
            </Text>
            <Text style={[styles.statDelta, { color: colors.textSecondary }]}>
              {isInvoicesLoading
                ? ''
                : `${invoiceSummary?.customerCount ?? 0} ${invoiceSummary?.customerCount === 1 ? 'customer' : 'customers'}`}
            </Text>
          </View>
          <View style={styles.statCard}>
            <Text style={styles.statLabel}>Forecast (30 days)</Text>
            <Text style={styles.statValue}>
              {isForecastLoading ? '…' : formatCurrency(forecastedEndBalance ?? 0, currency)}
            </Text>
            {forecastDelta && (
              <Text style={[styles.statDelta, { color: forecastDelta.positive ? colors.positive : colors.negative }]}>
                {forecastDelta.text}
              </Text>
            )}
          </View>
        </View>

        <View style={styles.sectionHeader}>
          <Text style={styles.sectionTitle}>Recent Transactions</Text>
          <Text style={styles.viewAll}>View all</Text>
        </View>

        <View style={styles.card}>
          {isLoading ? (
            <ActivityIndicator color={colors.primary} style={{ paddingVertical: 20 }} />
          ) : !summary || summary.recentTransactions.length === 0 ? (
            <Text style={{ color: colors.textMuted, textAlign: 'center', paddingVertical: 20 }}>No transactions yet.</Text>
          ) : (
            summary.recentTransactions.map((tx, i) => (
              <View key={tx.id} style={[styles.txRow, i === summary.recentTransactions.length - 1 && { borderBottomWidth: 0 }]}>
                <View style={[styles.txIcon, { backgroundColor: categoryColor(tx.category) + '33' }]}>
                  <CreditCard size={16} color={categoryColor(tx.category)} />
                </View>
                <View style={styles.txInfo}>
                  <Text style={styles.txTitle}>{tx.description}</Text>
                  <Text style={styles.txSubtitle}>
                    {categoryLabel(tx.category)} · {formatDate(tx.transactionDateUtc)}
                  </Text>
                </View>
                <Text style={[styles.txAmount, { color: tx.amount > 0 ? colors.positive : colors.negative }]}>
                  {tx.amount > 0 ? '+' : '-'}
                  {formatCurrency(Math.abs(tx.amount), tx.currency)}
                </Text>
              </View>
            ))
          )}
        </View>

        {!isInsightsLoading && insights.length > 0 && (
          <View style={styles.insightCard}>
            <View style={styles.insightIcon}>
              <Sparkles size={16} color={colors.textPrimary} />
            </View>
            <View style={styles.insightText}>
              <Text style={styles.insightTitle}>AI Insight</Text>
              <Text style={styles.insightBody}>{insights[0].title}</Text>
            </View>
            <ChevronRight size={18} color={colors.textSecondary} />
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  )
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.navy },
  content: { padding: 20, paddingBottom: 100 },
  header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 },
  greeting: { color: colors.textPrimary, fontSize: 22, fontWeight: '700' },
  subtitle: { color: colors.textSecondary, fontSize: 14, marginTop: 4, marginBottom: 20 },

  balanceCard: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.navyCard,
    borderRadius: 16,
    padding: 16,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: 12,
  },
  balanceIcon: {
    width: 36,
    height: 36,
    borderRadius: 10,
    backgroundColor: colors.primary + '33',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  balanceInfo: { flex: 1 },
  balanceLabel: { color: colors.textSecondary, fontSize: 12 },
  balanceValue: { color: colors.textPrimary, fontSize: 20, fontWeight: '700', marginTop: 2 },
  balanceDelta: { color: colors.positive, fontSize: 12, marginTop: 2 },

  statGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 12, marginBottom: 24 },
  statCard: {
    width: '47%',
    backgroundColor: colors.navyCard,
    borderRadius: 16,
    padding: 14,
    borderWidth: 1,
    borderColor: colors.border,
  },
  statLabel: { color: colors.textSecondary, fontSize: 12 },
  statValue: { color: colors.textPrimary, fontSize: 18, fontWeight: '700', marginTop: 4 },
  statDelta: { fontSize: 11, marginTop: 4 },

  sectionHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 },
  sectionTitle: { color: colors.textPrimary, fontSize: 16, fontWeight: '600' },
  viewAll: { color: colors.primary, fontSize: 13 },

  card: {
    backgroundColor: colors.navyCard,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: 14,
    marginBottom: 20,
  },
  txRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 14,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  txIcon: { width: 36, height: 36, borderRadius: 10, alignItems: 'center', justifyContent: 'center', marginRight: 12 },
  txInfo: { flex: 1 },
  txTitle: { color: colors.textPrimary, fontSize: 14, fontWeight: '600' },
  txSubtitle: { color: colors.textSecondary, fontSize: 12, marginTop: 2 },
  txAmount: { fontSize: 14, fontWeight: '600' },

  insightCard: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.navyLight,
    borderRadius: 16,
    padding: 14,
    borderWidth: 1,
    borderColor: colors.border,
  },
  insightIcon: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: colors.purple,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  insightText: { flex: 1 },
  insightTitle: { color: colors.textPrimary, fontSize: 13, fontWeight: '600' },
  insightBody: { color: colors.textSecondary, fontSize: 12, marginTop: 2 },
})
