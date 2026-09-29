import { colors } from '@/constants/colors'
import { useAuth } from '@/lib/AuthContext'
import { suggestCategory } from '@/lib/categorisationApi'
import { currencies } from '@/lib/currencies'
import { fetchExchangeRate } from '@/lib/exchangeRatesApi'
import { createTransaction } from '@/lib/transactionsApi'
import { transactionCategories } from '@/lib/types'
import { Sparkles, X } from 'lucide-react-native'
import { useEffect, useState } from 'react'
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

export function AddTransactionModal({ visible, onClose, onCreated }: { visible: boolean; onClose: () => void; onCreated: () => void }) {
  const { user } = useAuth()
  const companyCurrency = user?.companyCurrency ?? 'USD'
  const [description, setDescription] = useState('')
  const [category, setCategory] = useState<string>(transactionCategories[0])
  const [categoryTouched, setCategoryTouched] = useState(false)
  const [suggestedCategory, setSuggestedCategory] = useState<string | null>(null)
  const [amount, setAmount] = useState('')
  const [type, setType] = useState<'income' | 'expense'>('expense')
  const [currency, setCurrency] = useState(companyCurrency)
  const [exchangeRate, setExchangeRate] = useState('')
  const [rateIsLive, setRateIsLive] = useState(true)
  const [rateSource, setRateSource] = useState<string | null>(null)
  const [isFetchingRate, setIsFetchingRate] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

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

  useEffect(() => {
    if (description.trim().length < 3) return
    let cancelled = false
    const timer = setTimeout(() => {
      suggestCategory(description)
        .then((res) => {
          if (cancelled || res.confidence <= 0) return
          setSuggestedCategory(res.category)
          if (!categoryTouched) setCategory(res.category)
        })
        .catch(() => {
          // Best-effort suggestion — leave the manually-selected category untouched on failure.
        })
    }, 400)
    return () => {
      cancelled = true
      clearTimeout(timer)
    }
  }, [description, categoryTouched])

  async function handleSubmit() {
    setError(null)
    const numericAmount = Number(amount)
    if (!numericAmount || numericAmount <= 0) {
      setError('Enter an amount greater than zero.')
      return
    }

    const numericRate = exchangeRate ? Number(exchangeRate) : undefined
    if (currency !== companyCurrency && (!numericRate || numericRate <= 0)) {
      setError('Enter a valid exchange rate.')
      return
    }

    setIsSubmitting(true)
    try {
      await createTransaction({
        description,
        category,
        amount: type === 'income' ? numericAmount : -numericAmount,
        transactionDateUtc: new Date().toISOString(),
        status: 'Completed',
        currency,
        exchangeRate: currency === companyCurrency ? undefined : numericRate,
      })
      setDescription('')
      setAmount('')
      setCategory(transactionCategories[0])
      setCategoryTouched(false)
      setSuggestedCategory(null)
      setCurrency(companyCurrency)
      setExchangeRate('')
      onCreated()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create transaction')
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
                <Text style={styles.title}>Add Transaction</Text>
                <Pressable onPress={onClose}>
                  <X size={20} color={colors.textSecondary} />
                </Pressable>
              </View>

              <View style={styles.typeRow}>
                <Pressable
                  onPress={() => setType('income')}
                  style={[styles.typeButton, type === 'income' && styles.typeButtonIncomeActive]}
                >
                  <Text style={[styles.typeButtonText, type === 'income' && { color: colors.positive }]}>Income</Text>
                </Pressable>
                <Pressable
                  onPress={() => setType('expense')}
                  style={[styles.typeButton, type === 'expense' && styles.typeButtonExpenseActive]}
                >
                  <Text style={[styles.typeButtonText, type === 'expense' && { color: colors.negative }]}>Expense</Text>
                </Pressable>
              </View>

              <Text style={styles.label}>Description</Text>
              <TextInput style={styles.input} value={description} onChangeText={setDescription} placeholderTextColor={colors.textMuted} />

              <Text style={styles.label}>Category</Text>
              <View style={styles.categoryGrid}>
                {transactionCategories.map((c) => (
                  <Pressable
                    key={c}
                    onPress={() => {
                      setCategoryTouched(true)
                      setCategory(c)
                    }}
                    style={[styles.categoryChip, category === c && styles.categoryChipActive]}
                  >
                    <Text style={[styles.categoryChipText, category === c && { color: colors.textPrimary }]}>{c}</Text>
                  </Pressable>
                ))}
              </View>
              {suggestedCategory && !categoryTouched && description.trim().length >= 3 && (
                <View style={styles.suggestionRow}>
                  <Sparkles size={12} color={colors.purple} />
                  <Text style={styles.suggestionText}>Suggested from description</Text>
                </View>
              )}

              <Text style={styles.label}>Amount</Text>
              <TextInput
                style={styles.input}
                value={amount}
                onChangeText={setAmount}
                keyboardType="decimal-pad"
                placeholder="0.00"
                placeholderTextColor={colors.textMuted}
              />

              <Text style={styles.label}>Currency</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.categoryGrid}>
                {currencies.map((c) => (
                  <Pressable
                    key={c.code}
                    onPress={() => handleCurrencyChange(c.code)}
                    style={[styles.categoryChip, currency === c.code && styles.categoryChipActive]}
                  >
                    <Text style={[styles.categoryChipText, currency === c.code && { color: colors.textPrimary }]}>{c.code}</Text>
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
                  {isFetchingRate && <Text style={styles.suggestionText}>Fetching live rate…</Text>}
                  {!isFetchingRate && !rateIsLive && (
                    <Text style={styles.error}>Could not fetch a live rate — please enter it manually.</Text>
                  )}
                  {rateSource === 'ExchangeRate-API' && (
                    <Text style={styles.suggestionText} onPress={() => Linking.openURL('https://www.exchangerate-api.com')}>
                      Rates By Exchange Rate API
                    </Text>
                  )}
                </>
              )}

              {error && <Text style={styles.error}>{error}</Text>}

              <Pressable style={styles.submitButton} onPress={handleSubmit} disabled={isSubmitting}>
                {isSubmitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.submitButtonText}>Add Transaction</Text>}
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
  typeRow: { flexDirection: 'row', gap: 10, marginBottom: 16 },
  typeButton: {
    flex: 1,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: colors.border,
    paddingVertical: 10,
    alignItems: 'center',
  },
  typeButtonIncomeActive: { borderColor: colors.positive, backgroundColor: colors.positive + '1A' },
  typeButtonExpenseActive: { borderColor: colors.negative, backgroundColor: colors.negative + '1A' },
  typeButtonText: { color: colors.textSecondary, fontWeight: '600', fontSize: 13 },
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
  categoryGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  categoryChip: {
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: 12,
    paddingVertical: 7,
  },
  categoryChipActive: { borderColor: colors.primary, backgroundColor: colors.primary + '33' },
  categoryChipText: { color: colors.textSecondary, fontSize: 12, fontWeight: '500' },
  suggestionRow: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 6 },
  suggestionText: { color: colors.purple, fontSize: 11 },
  error: { color: colors.negative, fontSize: 13, marginTop: 12 },
  submitButton: { backgroundColor: colors.primary, borderRadius: 10, paddingVertical: 14, alignItems: 'center', marginTop: 20, marginBottom: 8 },
  submitButtonText: { color: '#fff', fontSize: 15, fontWeight: '600' },
})
