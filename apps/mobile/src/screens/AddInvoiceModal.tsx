import { colors } from '@/constants/colors'
import { useAuth } from '@/lib/AuthContext'
import { formatCurrency } from '@/lib/categoryDisplay'
import { currencies } from '@/lib/currencies'
import { fetchExchangeRate } from '@/lib/exchangeRatesApi'
import { createInvoice } from '@/lib/invoicesApi'
import { Plus, X } from 'lucide-react-native'
import { useState } from 'react'
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Linking,
  Modal,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native'

function toIsoDate(date: Date) {
  return date.toISOString().slice(0, 10)
}

interface LineItemRow {
  description: string
  amount: string
}

const EMPTY_LINE_ITEM: LineItemRow = { description: '', amount: '' }

export function AddInvoiceModal({ visible, onClose, onCreated }: { visible: boolean; onClose: () => void; onCreated: () => void }) {
  const { user } = useAuth()
  const companyCurrency = user?.companyCurrency ?? 'USD'
  const [customerName, setCustomerName] = useState('')
  const [lineItems, setLineItems] = useState<LineItemRow[]>([{ ...EMPTY_LINE_ITEM }])
  const [notes, setNotes] = useState('')
  const [issueDate, setIssueDate] = useState(() => toIsoDate(new Date()))
  const [dueDate, setDueDate] = useState(() => toIsoDate(new Date(Date.now() + 14 * 86400000)))
  const [currency, setCurrency] = useState(companyCurrency)
  const [exchangeRate, setExchangeRate] = useState('')
  const [rateIsLive, setRateIsLive] = useState(true)
  const [rateSource, setRateSource] = useState<string | null>(null)
  const [isFetchingRate, setIsFetchingRate] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const total = lineItems.reduce((sum, li) => sum + (Number(li.amount) || 0), 0)

  function updateLineItem(index: number, patch: Partial<LineItemRow>) {
    setLineItems((rows) => rows.map((row, i) => (i === index ? { ...row, ...patch } : row)))
  }

  function addLineItem() {
    setLineItems((rows) => [...rows, { ...EMPTY_LINE_ITEM }])
  }

  function removeLineItem(index: number) {
    setLineItems((rows) => rows.filter((_, i) => i !== index))
  }

  function handleCurrencyChange(newCurrency: string) {
    setCurrency(newCurrency)
    if (newCurrency === companyCurrency) {
      setExchangeRate('')
      setRateIsLive(true)
      return
    }
    setIsFetchingRate(true)
    fetchExchangeRate(newCurrency, companyCurrency)
      .then((res) => {
        setRateIsLive(res.isLive)
        setRateSource(res.source)
        setExchangeRate(res.rate !== null ? String(res.rate) : '')
      })
      .catch(() => {
        setRateIsLive(false)
        setRateSource(null)
        setExchangeRate('')
      })
      .finally(() => setIsFetchingRate(false))
  }

  async function handleSubmit() {
    setError(null)

    const cleanedLineItems = lineItems
      .map((li) => ({ description: li.description.trim(), amount: Number(li.amount) }))
      .filter((li) => li.description.length > 0 || li.amount > 0)

    if (cleanedLineItems.length === 0) {
      setError('Add at least one line item.')
      return
    }
    const invalidRow = cleanedLineItems.find((li) => !li.description || !li.amount || li.amount <= 0)
    if (invalidRow) {
      setError('Every line item needs a description and an amount greater than zero.')
      return
    }

    const numericRate = exchangeRate ? Number(exchangeRate) : undefined
    if (currency !== companyCurrency && (!numericRate || numericRate <= 0)) {
      setError('Enter a valid exchange rate.')
      return
    }

    setIsSubmitting(true)
    try {
      await createInvoice({
        customerName,
        lineItems: cleanedLineItems,
        issueDateUtc: new Date(issueDate).toISOString(),
        dueDateUtc: new Date(dueDate).toISOString(),
        currency,
        exchangeRate: currency === companyCurrency ? undefined : numericRate,
        notes: notes.trim() || undefined,
      })
      setCustomerName('')
      setLineItems([{ ...EMPTY_LINE_ITEM }])
      setNotes('')
      setCurrency(companyCurrency)
      setExchangeRate('')
      onCreated()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create invoice')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal visible={visible} animationType="slide" transparent onRequestClose={onClose}>
      <View style={styles.overlay}>
        <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : 'height'} style={styles.sheetWrap}>
          <View style={styles.sheet}>
            <ScrollView keyboardShouldPersistTaps="handled">
              <View style={styles.header}>
                <Text style={styles.title}>New Invoice</Text>
                <Pressable onPress={onClose}>
                  <X size={20} color={colors.textSecondary} />
                </Pressable>
              </View>

              <Text style={styles.label}>Customer</Text>
              <TextInput style={styles.input} value={customerName} onChangeText={setCustomerName} placeholderTextColor={colors.textMuted} />

              <Text style={styles.label}>Line Items</Text>
              {lineItems.map((row, i) => (
                <View key={i} style={styles.lineItemRow}>
                  <TextInput
                    style={[styles.input, styles.lineItemDescription]}
                    value={row.description}
                    onChangeText={(text) => updateLineItem(i, { description: text })}
                    placeholder="Description"
                    placeholderTextColor={colors.textMuted}
                  />
                  <TextInput
                    style={[styles.input, styles.lineItemAmount]}
                    value={row.amount}
                    onChangeText={(text) => updateLineItem(i, { amount: text })}
                    keyboardType="decimal-pad"
                    placeholder="0.00"
                    placeholderTextColor={colors.textMuted}
                  />
                  <Pressable onPress={() => removeLineItem(i)} disabled={lineItems.length === 1} hitSlop={8}>
                    <X size={16} color={lineItems.length === 1 ? 'transparent' : colors.textMuted} />
                  </Pressable>
                </View>
              ))}
              <Pressable style={styles.addLineItemButton} onPress={addLineItem}>
                <Plus size={14} color={colors.primary} />
                <Text style={styles.addLineItemText}>Add line item</Text>
              </Pressable>
              <Text style={styles.total}>Total: {formatCurrency(total, currency)}</Text>

              <Text style={styles.label}>Notes / Terms</Text>
              <TextInput
                style={[styles.input, styles.notesInput]}
                value={notes}
                onChangeText={setNotes}
                placeholder="Optional — payment terms, thank-you note, etc."
                placeholderTextColor={colors.textMuted}
                multiline
              />

              <Text style={styles.label}>Currency</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.currencyGrid}>
                {currencies.map((c) => (
                  <Pressable
                    key={c.code}
                    onPress={() => handleCurrencyChange(c.code)}
                    style={[styles.currencyChip, currency === c.code && styles.currencyChipActive]}
                  >
                    <Text style={[styles.currencyChipText, currency === c.code && { color: colors.textPrimary }]}>{c.code}</Text>
                  </Pressable>
                ))}
              </ScrollView>

              {currency !== companyCurrency && (
                <>
                  <Text style={styles.label}>Exchange rate to {companyCurrency}</Text>
                  <TextInput
                    style={styles.input}
                    value={exchangeRate}
                    onChangeText={setExchangeRate}
                    keyboardType="decimal-pad"
                    editable={!isFetchingRate}
                    placeholderTextColor={colors.textMuted}
                  />
                  {isFetchingRate && <Text style={styles.hint}>Fetching live rate…</Text>}
                  {!isFetchingRate && !rateIsLive && (
                    <Text style={styles.error}>Could not fetch a live rate — please enter it manually.</Text>
                  )}
                  {rateSource === 'ExchangeRate-API' && (
                    <Text style={styles.hint} onPress={() => Linking.openURL('https://www.exchangerate-api.com')}>
                      Rates By Exchange Rate API
                    </Text>
                  )}
                </>
              )}

              <Text style={styles.label}>Issue Date (YYYY-MM-DD)</Text>
              <TextInput style={styles.input} value={issueDate} onChangeText={setIssueDate} placeholderTextColor={colors.textMuted} />

              <Text style={styles.label}>Due Date (YYYY-MM-DD)</Text>
              <TextInput style={styles.input} value={dueDate} onChangeText={setDueDate} placeholderTextColor={colors.textMuted} />

              {error && <Text style={styles.error}>{error}</Text>}

              <Pressable style={styles.submitButton} onPress={handleSubmit} disabled={isSubmitting}>
                {isSubmitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.submitButtonText}>Create Invoice</Text>}
              </Pressable>
            </ScrollView>
          </View>
        </KeyboardAvoidingView>
      </View>
    </Modal>
  )
}

const styles = StyleSheet.create({
  overlay: { flex: 1, backgroundColor: 'rgba(0,0,0,0.5)', justifyContent: 'flex-end' },
  sheetWrap: { width: '100%' },
  sheet: { backgroundColor: colors.navyLight, borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 20, maxHeight: '85%' },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 },
  title: { color: colors.textPrimary, fontSize: 18, fontWeight: '700' },
  label: { color: colors.textSecondary, fontSize: 13, fontWeight: '500', marginBottom: 6, marginTop: 12 },
  input: {
    backgroundColor: colors.navyCard,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    color: colors.textPrimary,
    fontSize: 15,
  },
  notesInput: { minHeight: 60, textAlignVertical: 'top' },
  lineItemRow: { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 8 },
  lineItemDescription: { flex: 1 },
  lineItemAmount: { width: 90 },
  addLineItemButton: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 2 },
  addLineItemText: { color: colors.primary, fontSize: 12, fontWeight: '600' },
  total: { color: colors.textPrimary, fontSize: 13, fontWeight: '700', textAlign: 'right', marginTop: 8 },
  currencyGrid: { flexDirection: 'row', gap: 8 },
  currencyChip: {
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: 12,
    paddingVertical: 7,
  },
  currencyChipActive: { borderColor: colors.primary, backgroundColor: colors.primary + '33' },
  currencyChipText: { color: colors.textSecondary, fontSize: 12, fontWeight: '500' },
  hint: { color: colors.textMuted, fontSize: 11, marginTop: 6 },
  error: { color: colors.negative, fontSize: 13, marginTop: 12 },
  submitButton: { backgroundColor: colors.primary, borderRadius: 10, paddingVertical: 14, alignItems: 'center', marginTop: 20, marginBottom: 8 },
  submitButtonText: { color: '#fff', fontSize: 15, fontWeight: '600' },
})
