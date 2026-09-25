import { colors } from '@/constants/colors'
import { createInvitation } from '@/lib/teamApi'
import { teamRoles } from '@/lib/types'
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

export function InviteMemberModal({ visible, onClose, onCreated }: { visible: boolean; onClose: () => void; onCreated: () => void }) {
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<string>('Member')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit() {
    setError(null)
    setIsSubmitting(true)
    try {
      await createInvitation(email, role)
      setEmail('')
      onCreated()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send invitation')
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
                <Text style={styles.title}>Invite Team Member</Text>
                <Pressable onPress={onClose}>
                  <X size={20} color={colors.textSecondary} />
                </Pressable>
              </View>

              <Text style={styles.label}>Email</Text>
              <TextInput
                style={styles.input}
                value={email}
                onChangeText={setEmail}
                autoCapitalize="none"
                keyboardType="email-address"
                placeholderTextColor={colors.textMuted}
              />

              <Text style={styles.label}>Role</Text>
              <View style={styles.roleRow}>
                {teamRoles
                  .filter((r) => r !== 'Owner')
                  .map((r) => (
                    <Pressable key={r} onPress={() => setRole(r)} style={[styles.roleChip, role === r && styles.roleChipActive]}>
                      <Text style={[styles.roleChipText, role === r && { color: colors.textPrimary }]}>{r}</Text>
                    </Pressable>
                  ))}
              </View>

              {error && <Text style={styles.error}>{error}</Text>}

              <Pressable style={styles.submitButton} onPress={handleSubmit} disabled={isSubmitting}>
                {isSubmitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.submitButtonText}>Send Invitation</Text>}
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
  roleRow: { flexDirection: 'row', gap: 8 },
  roleChip: { borderRadius: 999, borderWidth: 1, borderColor: colors.border, paddingHorizontal: 14, paddingVertical: 8 },
  roleChipActive: { borderColor: colors.primary, backgroundColor: colors.primary + '33' },
  roleChipText: { color: colors.textSecondary, fontSize: 13, fontWeight: '500' },
  error: { color: colors.negative, fontSize: 13, marginTop: 12 },
  submitButton: { backgroundColor: colors.primary, borderRadius: 10, paddingVertical: 14, alignItems: 'center', marginTop: 20, marginBottom: 8 },
  submitButtonText: { color: '#fff', fontSize: 15, fontWeight: '600' },
})
