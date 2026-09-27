import { colors } from '@/constants/colors'
import { sendFeedback } from '@/lib/feedbackApi'
import { X } from 'lucide-react-native'
import { useState } from 'react'
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Modal,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native'

export function FeedbackModal({ visible, onClose }: { visible: boolean; onClose: () => void }) {
  const [message, setMessage] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [sent, setSent] = useState(false)

  function handleClose() {
    setMessage('')
    setError(null)
    setSent(false)
    onClose()
  }

  async function handleSubmit() {
    setError(null)
    setIsSubmitting(true)
    try {
      await sendFeedback(message)
      setSent(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send feedback')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal visible={visible} animationType="slide" transparent onRequestClose={handleClose}>
      <View style={styles.overlay}>
        <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : 'height'} style={styles.sheetWrap}>
          <View style={styles.sheet}>
            <ScrollView keyboardShouldPersistTaps="handled">
              <View style={styles.header}>
                <Text style={styles.title}>Send Feedback</Text>
                <Pressable onPress={handleClose}>
                  <X size={20} color={colors.textSecondary} />
                </Pressable>
              </View>

              {sent ? (
                <View style={{ alignItems: 'center', gap: 12, paddingVertical: 16 }}>
                  <Text style={{ color: colors.textSecondary, fontSize: 14, textAlign: 'center' }}>
                    Thanks! Your feedback has been sent.
                  </Text>
                  <Pressable style={styles.submitButton} onPress={handleClose}>
                    <Text style={styles.submitButtonText}>Close</Text>
                  </Pressable>
                </View>
              ) : (
                <>
                  <Text style={styles.label}>What&apos;s on your mind?</Text>
                  <TextInput
                    style={styles.textarea}
                    value={message}
                    onChangeText={setMessage}
                    placeholder="Bug reports, feature ideas, anything at all…"
                    placeholderTextColor={colors.textMuted}
                    multiline
                    numberOfLines={5}
                    maxLength={2000}
                  />

                  {error && <Text style={styles.error}>{error}</Text>}

                  <Pressable style={styles.submitButton} onPress={handleSubmit} disabled={isSubmitting || !message.trim()}>
                    {isSubmitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.submitButtonText}>Send Feedback</Text>}
                  </Pressable>
                </>
              )}
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
  textarea: {
    backgroundColor: colors.navyCard,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    color: colors.textPrimary,
    fontSize: 15,
    minHeight: 100,
    textAlignVertical: 'top',
  },
  error: { color: colors.negative, fontSize: 13, marginTop: 12 },
  submitButton: { backgroundColor: colors.primary, borderRadius: 10, paddingVertical: 14, alignItems: 'center', marginTop: 20, marginBottom: 8 },
  submitButtonText: { color: '#fff', fontSize: 15, fontWeight: '600' },
})
