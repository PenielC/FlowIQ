import { colors } from '@/constants/colors'
import { useAuth } from '@/lib/AuthContext'
import { formatCurrency, formatDate } from '@/lib/categoryDisplay'
import { fetchCompanyLogo } from '@/lib/companiesApi'
import { invoiceStatusColors, invoiceStatusLabel } from '@/lib/invoiceDisplay'
import { fetchInvoiceById, markInvoicePaid } from '@/lib/invoicesApi'
import type { InvoiceResponse } from '@/lib/types'
import { Building2, Calendar, CheckCircle2, Clock, X } from 'lucide-react-native'
import { useEffect, useState } from 'react'
import { ActivityIndicator, Image, Modal, Pressable, StyleSheet, Text, View } from 'react-native'

function dueDateNote(invoice: InvoiceResponse) {
  if (invoice.status === 'Paid') return null

  const daysUntilDue = Math.ceil((new Date(invoice.dueDateUtc).getTime() - Date.now()) / 86400000)
  if (daysUntilDue < 0) return { text: `${Math.abs(daysUntilDue)} day${Math.abs(daysUntilDue) === 1 ? '' : 's'} overdue`, color: colors.negative }
  if (daysUntilDue === 0) return { text: 'Due today', color: colors.warning }
  return { text: `Due in ${daysUntilDue} day${daysUntilDue === 1 ? '' : 's'}`, color: colors.textSecondary }
}

function InvoiceDetailContent({
  invoiceId,
  onClose,
  onChanged,
}: {
  invoiceId: string
  onClose: () => void
  onChanged: () => void
}) {
  const { user } = useAuth()
  const [invoice, setInvoice] = useState<InvoiceResponse | null>(null)
  const [logoDataUrl, setLogoDataUrl] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isMarkingPaid, setIsMarkingPaid] = useState(false)

  useEffect(() => {
    let cancelled = false
    Promise.all([fetchInvoiceById(invoiceId), fetchCompanyLogo().catch(() => ({ dataUrl: null }))])
      .then(([invoiceData, logo]) => {
        if (cancelled) return
        setInvoice(invoiceData)
        setLogoDataUrl(logo.dataUrl)
      })
      .catch((err) => !cancelled && setError(err instanceof Error ? err.message : 'Failed to load invoice'))
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [invoiceId])

  async function handleMarkPaid() {
    setIsMarkingPaid(true)
    try {
      const updated = await markInvoicePaid(invoiceId)
      setInvoice(updated)
      onChanged()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to mark invoice as paid')
    } finally {
      setIsMarkingPaid(false)
    }
  }

  const note = invoice ? dueDateNote(invoice) : null
  const statusColors = invoice ? invoiceStatusColors(invoice.status) : null

  return (
    <View style={styles.sheet}>
      <View style={styles.header}>
        <Text style={styles.title}>Invoice Details</Text>
        <Pressable onPress={onClose} hitSlop={8}>
          <X size={20} color={colors.textSecondary} />
        </Pressable>
      </View>

      {isLoading ? (
        <ActivityIndicator color={colors.primary} style={{ marginVertical: 32 }} />
      ) : error && !invoice ? (
        <Text style={styles.errorCentered}>{error}</Text>
      ) : invoice && statusColors ? (
        <View>
          <View style={styles.companyRow}>
            {logoDataUrl ? (
              <Image source={{ uri: logoDataUrl }} style={styles.companyLogo} resizeMode="contain" />
            ) : (
              <Building2 size={18} color={colors.textMuted} />
            )}
            <View>
              <Text style={styles.companyName}>{user?.companyName}</Text>
              <Text style={styles.invoiceNumber}>INV-{invoice.id.slice(0, 8).toUpperCase()}</Text>
            </View>
          </View>

          <View style={styles.amountRow}>
            <View>
              <Text style={styles.amount}>{formatCurrency(invoice.amount, invoice.currency)}</Text>
              <View style={[styles.statusPill, { backgroundColor: statusColors.bg }]}>
                <Text style={[styles.statusText, { color: statusColors.text }]}>{invoiceStatusLabel(invoice.status)}</Text>
              </View>
            </View>
            {note && <Text style={[styles.note, { color: note.color }]}>{note.text}</Text>}
          </View>

          <View style={styles.detailCard}>
            <View style={styles.detailRow}>
              <Building2 size={16} color={colors.textMuted} />
              <View>
                <Text style={styles.detailLabel}>Customer</Text>
                <Text style={styles.detailValue}>{invoice.customerName}</Text>
              </View>
            </View>
            <View style={styles.detailRow}>
              <Calendar size={16} color={colors.textMuted} />
              <View>
                <Text style={styles.detailLabel}>Issue Date</Text>
                <Text style={styles.detailValue}>{formatDate(invoice.issueDateUtc)}</Text>
              </View>
            </View>
            <View style={styles.detailRow}>
              <Clock size={16} color={colors.textMuted} />
              <View>
                <Text style={styles.detailLabel}>Due Date</Text>
                <Text style={styles.detailValue}>{formatDate(invoice.dueDateUtc)}</Text>
              </View>
            </View>
          </View>

          <View style={styles.lineItemsCard}>
            {invoice.lineItems.map((li) => (
              <View key={li.id} style={styles.lineItemRow}>
                <Text style={styles.lineItemDescription}>{li.description}</Text>
                <Text style={styles.lineItemAmount}>{formatCurrency(li.amount, invoice.currency)}</Text>
              </View>
            ))}
          </View>

          {invoice.notes && (
            <View style={styles.notesBlock}>
              <Text style={styles.detailLabel}>Notes / Terms</Text>
              <Text style={styles.notesText}>{invoice.notes}</Text>
            </View>
          )}

          {error && <Text style={styles.error}>{error}</Text>}

          {invoice.status !== 'Paid' && (
            <Pressable style={styles.payButton} onPress={handleMarkPaid} disabled={isMarkingPaid}>
              {isMarkingPaid ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <>
                  <CheckCircle2 size={16} color="#fff" />
                  <Text style={styles.payButtonText}>Mark as Paid</Text>
                </>
              )}
            </Pressable>
          )}
        </View>
      ) : null}
    </View>
  )
}

export function InvoiceDetailModal({
  invoiceId,
  onClose,
  onChanged,
}: {
  invoiceId: string | null
  onClose: () => void
  onChanged: () => void
}) {
  return (
    <Modal visible={invoiceId !== null} animationType="slide" transparent onRequestClose={onClose}>
      <View style={styles.overlay}>
        {invoiceId && <InvoiceDetailContent key={invoiceId} invoiceId={invoiceId} onClose={onClose} onChanged={onChanged} />}
      </View>
    </Modal>
  )
}

const styles = StyleSheet.create({
  overlay: { flex: 1, backgroundColor: 'rgba(0,0,0,0.5)', justifyContent: 'flex-end' },
  sheet: { backgroundColor: colors.navyLight, borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 20, paddingBottom: 32 },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 },
  title: { color: colors.textPrimary, fontSize: 18, fontWeight: '700' },
  errorCentered: { color: colors.negative, textAlign: 'center', marginVertical: 32 },
  companyRow: { flexDirection: 'row', alignItems: 'center', gap: 10, marginBottom: 16 },
  companyLogo: { width: 28, height: 28 },
  companyName: { color: colors.textPrimary, fontSize: 14, fontWeight: '700' },
  invoiceNumber: { color: colors.textMuted, fontSize: 11, marginTop: 1 },
  amountRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start' },
  amount: { color: colors.textPrimary, fontSize: 26, fontWeight: '700' },
  statusPill: { marginTop: 6, alignSelf: 'flex-start', paddingHorizontal: 8, paddingVertical: 3, borderRadius: 999 },
  statusText: { fontSize: 12, fontWeight: '600' },
  note: { fontSize: 13, fontWeight: '500' },
  detailCard: {
    marginTop: 20,
    backgroundColor: colors.navyCard,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 14,
    padding: 14,
    gap: 14,
  },
  detailRow: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  detailLabel: { color: colors.textMuted, fontSize: 12 },
  detailValue: { color: colors.textPrimary, fontSize: 14, fontWeight: '600', marginTop: 1 },
  lineItemsCard: {
    marginTop: 14,
    backgroundColor: colors.navyCard,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 14,
    padding: 14,
    gap: 10,
  },
  lineItemRow: { flexDirection: 'row', justifyContent: 'space-between', gap: 10 },
  lineItemDescription: { flex: 1, color: colors.textPrimary, fontSize: 13 },
  lineItemAmount: { color: colors.textPrimary, fontSize: 13, fontWeight: '600' },
  notesBlock: { marginTop: 14 },
  notesText: { color: colors.textSecondary, fontSize: 13, marginTop: 4 },
  error: { color: colors.negative, fontSize: 13, marginTop: 12 },
  payButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: colors.primary,
    borderRadius: 10,
    paddingVertical: 14,
    marginTop: 20,
  },
  payButtonText: { color: '#fff', fontSize: 15, fontWeight: '600' },
})
