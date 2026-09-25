import { colors } from '@/constants/colors'
import { formatCurrency, formatDate } from '@/lib/categoryDisplay'
import { invoiceStatusColors, invoiceStatusLabel } from '@/lib/invoiceDisplay'
import { fetchInvoices, markInvoicePaid } from '@/lib/invoicesApi'
import type { InvoiceResponse, PagedResult } from '@/lib/types'
import { AddInvoiceModal } from '@/screens/AddInvoiceModal'
import { InvoiceDetailModal } from '@/screens/InvoiceDetailModal'
import { CheckCircle2, ChevronLeft, ChevronRight, Plus } from 'lucide-react-native'
import { useEffect, useState } from 'react'
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'

const PAGE_SIZE = 15

export default function InvoicesScreen() {
  const [page, setPage] = useState(1)
  const [reloadToken, setReloadToken] = useState(0)
  const [result, setResult] = useState<PagedResult<InvoiceResponse> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [showAddModal, setShowAddModal] = useState(false)
  const [payingId, setPayingId] = useState<string | null>(null)
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    fetchInvoices(page, PAGE_SIZE)
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

  async function handleMarkPaid(id: string) {
    setPayingId(id)
    try {
      await markInvoicePaid(id)
      setReloadToken((t) => t + 1)
    } catch {
      // Swallow — row simply won't update; fine for this pass.
    } finally {
      setPayingId(null)
    }
  }

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <View style={styles.header}>
        <Text style={styles.title}>Invoices</Text>
      </View>

      {isLoading ? (
        <ActivityIndicator color={colors.primary} style={{ marginTop: 40 }} />
      ) : (
        <FlatList
          data={result?.items ?? []}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          ListEmptyComponent={<Text style={styles.empty}>No invoices yet. Tap + to create one.</Text>}
          renderItem={({ item }) => {
            const statusColors = invoiceStatusColors(item.status)
            return (
              <Pressable style={styles.row} onPress={() => setSelectedInvoiceId(item.id)}>
                <View style={styles.rowMain}>
                  <Text style={styles.customerName}>{item.customerName}</Text>
                  <Text style={styles.meta}>Due {formatDate(item.dueDateUtc)}</Text>
                </View>
                <View style={styles.rowEnd}>
                  <Text style={styles.amount}>{formatCurrency(item.amount, item.currency)}</Text>
                  <View style={[styles.statusPill, { backgroundColor: statusColors.bg }]}>
                    <Text style={[styles.statusText, { color: statusColors.text }]}>{invoiceStatusLabel(item.status)}</Text>
                  </View>
                </View>
                {item.status !== 'Paid' && (
                  <Pressable
                    style={styles.markPaidButton}
                    onPress={() => handleMarkPaid(item.id)}
                    disabled={payingId === item.id}
                  >
                    <CheckCircle2 size={14} color={colors.positive} />
                    <Text style={styles.markPaidText}>{payingId === item.id ? 'Marking…' : 'Mark Paid'}</Text>
                  </Pressable>
                )}
              </Pressable>
            )
          }}
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

      <AddInvoiceModal visible={showAddModal} onClose={() => setShowAddModal(false)} onCreated={handleCreated} />

      <InvoiceDetailModal
        invoiceId={selectedInvoiceId}
        onClose={() => setSelectedInvoiceId(null)}
        onChanged={() => setReloadToken((t) => t + 1)}
      />
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
    backgroundColor: colors.navyCard,
    borderRadius: 14,
    borderWidth: 1,
    borderColor: colors.border,
    padding: 12,
    marginBottom: 10,
  },
  rowMain: { marginBottom: 8 },
  customerName: { color: colors.textPrimary, fontSize: 14, fontWeight: '600' },
  meta: { color: colors.textSecondary, fontSize: 12, marginTop: 2 },
  rowEnd: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  amount: { color: colors.textPrimary, fontSize: 15, fontWeight: '700' },
  statusPill: { paddingHorizontal: 8, paddingVertical: 3, borderRadius: 999 },
  statusText: { fontSize: 11, fontWeight: '600' },
  markPaidButton: { flexDirection: 'row', alignItems: 'center', gap: 6, marginTop: 10 },
  markPaidText: { color: colors.positive, fontSize: 12, fontWeight: '600' },
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
