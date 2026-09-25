import { AddTransactionModal } from '@/screens/AddTransactionModal'
import { colors } from '@/constants/colors'
import { categoryColor, categoryLabel, formatCurrency, formatDate } from '@/lib/categoryDisplay'
import { fetchTransactions } from '@/lib/transactionsApi'
import type { PagedResult, TransactionResponse } from '@/lib/types'
import { ChevronLeft, ChevronRight, CreditCard, Plus } from 'lucide-react-native'
import { useEffect, useState } from 'react'
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'

const PAGE_SIZE = 15

export default function TransactionsScreen() {
  const [page, setPage] = useState(1)
  const [reloadToken, setReloadToken] = useState(0)
  const [result, setResult] = useState<PagedResult<TransactionResponse> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [showAddModal, setShowAddModal] = useState(false)

  useEffect(() => {
    let cancelled = false
    fetchTransactions(page, PAGE_SIZE)
      .then((r) => !cancelled && setResult(r))
      .catch(() => !cancelled && setResult(null))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [page, reloadToken])

  function goToPage(next: number) {
    setIsLoading(true)
    setPage(next)
  }

  function handleCreated() {
    setShowAddModal(false)
    setIsLoading(true)
    setPage(1)
    setReloadToken((t) => t + 1)
  }

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <View style={styles.header}>
        <Text style={styles.title}>Transactions</Text>
      </View>

      {isLoading ? (
        <ActivityIndicator color={colors.primary} style={{ marginTop: 40 }} />
      ) : (
        <FlatList
          data={result?.items ?? []}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          ListEmptyComponent={<Text style={styles.empty}>No transactions yet. Tap + to add one.</Text>}
          renderItem={({ item }) => (
            <View style={styles.row}>
              <View style={[styles.icon, { backgroundColor: categoryColor(item.category) + '33' }]}>
                <CreditCard size={16} color={categoryColor(item.category)} />
              </View>
              <View style={styles.info}>
                <Text style={styles.description}>{item.description}</Text>
                <Text style={styles.meta}>
                  {categoryLabel(item.category)} · {formatDate(item.transactionDateUtc)}
                </Text>
              </View>
              <Text style={[styles.amount, { color: item.amount > 0 ? colors.positive : colors.negative }]}>
                {item.amount > 0 ? '+' : '-'}
                {formatCurrency(Math.abs(item.amount), item.currency)}
              </Text>
            </View>
          )}
          ListFooterComponent={
            result && result.totalCount > 0 ? (
              <View style={styles.pagination}>
                <Text style={styles.pageInfo}>
                  Page {result.pageNumber} of {result.totalPages} · {result.totalCount} total
                </Text>
                <View style={styles.pageButtons}>
                  <Pressable
                    disabled={!result.hasPreviousPage}
                    onPress={() => goToPage(page - 1)}
                    style={[styles.pageButton, !result.hasPreviousPage && styles.pageButtonDisabled]}
                  >
                    <ChevronLeft size={16} color={colors.textSecondary} />
                  </Pressable>
                  <Pressable
                    disabled={!result.hasNextPage}
                    onPress={() => goToPage(page + 1)}
                    style={[styles.pageButton, !result.hasNextPage && styles.pageButtonDisabled]}
                  >
                    <ChevronRight size={16} color={colors.textSecondary} />
                  </Pressable>
                </View>
              </View>
            ) : null
          }
        />
      )}

      <Pressable style={styles.fab} onPress={() => setShowAddModal(true)}>
        <Plus size={22} color="#fff" />
      </Pressable>

      <AddTransactionModal visible={showAddModal} onClose={() => setShowAddModal(false)} onCreated={handleCreated} />
    </SafeAreaView>
  )
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.navy },
  header: { paddingHorizontal: 20, paddingTop: 8, paddingBottom: 16 },
  title: { color: colors.textPrimary, fontSize: 22, fontWeight: '700' },
  listContent: { paddingHorizontal: 20, paddingBottom: 100 },
  empty: { color: colors.textMuted, textAlign: 'center', marginTop: 40 },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.navyCard,
    borderRadius: 14,
    borderWidth: 1,
    borderColor: colors.border,
    padding: 12,
    marginBottom: 10,
  },
  icon: { width: 36, height: 36, borderRadius: 10, alignItems: 'center', justifyContent: 'center', marginRight: 12 },
  info: { flex: 1 },
  description: { color: colors.textPrimary, fontSize: 14, fontWeight: '600' },
  meta: { color: colors.textSecondary, fontSize: 12, marginTop: 2 },
  amount: { fontSize: 14, fontWeight: '600' },
  pagination: { alignItems: 'center', marginTop: 8, gap: 10 },
  pageInfo: { color: colors.textMuted, fontSize: 12 },
  pageButtons: { flexDirection: 'row', gap: 10 },
  pageButton: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 8,
    padding: 8,
  },
  pageButtonDisabled: { opacity: 0.4 },
  fab: {
    position: 'absolute',
    right: 20,
    bottom: 24,
    width: 52,
    height: 52,
    borderRadius: 26,
    backgroundColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: colors.primary,
    shadowOpacity: 0.5,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 4 },
    elevation: 6,
  },
})
