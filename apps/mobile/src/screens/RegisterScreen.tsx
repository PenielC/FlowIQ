import { IconTextField } from '@/components/IconTextField'
import { Logo } from '@/components/Logo'
import { PasswordField } from '@/components/PasswordField'
import { colors } from '@/constants/colors'
import { useAuth } from '@/lib/AuthContext'
import { ArrowLeft, Building2, Mail, User } from 'lucide-react-native'
import { useState } from 'react'
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'

export function RegisterScreen({ onSwitchToLogin }: { onSwitchToLogin: () => void }) {
  const { register } = useAuth()
  const [companyName, setCompanyName] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit() {
    setError(null)
    setIsSubmitting(true)
    try {
      await register({ companyName, firstName, lastName, email, password })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <SafeAreaView style={styles.safe}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : 'height'} style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
          <Pressable onPress={onSwitchToLogin} style={styles.backLink} hitSlop={8}>
            <ArrowLeft size={16} color={colors.textMuted} />
            <Text style={styles.backText}>Back to login</Text>
          </Pressable>

          <View style={styles.logoWrap}>
            <Logo source={require('@/assets/images/flowiq-logo-v2-dark.png')} aspectRatio={1895 / 756} />
          </View>

          <Text style={styles.title}>Create your account</Text>
          <Text style={styles.subtitle}>Start predicting your cash flow with FlowIQ.</Text>

          <Text style={styles.label}>Company name</Text>
          <IconTextField icon={Building2} value={companyName} onChangeText={setCompanyName} />

          <Text style={styles.label}>First name</Text>
          <IconTextField icon={User} value={firstName} onChangeText={setFirstName} />

          <Text style={styles.label}>Last name</Text>
          <IconTextField icon={User} value={lastName} onChangeText={setLastName} />

          <Text style={styles.label}>Email Address</Text>
          <IconTextField
            icon={Mail}
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            keyboardType="email-address"
            placeholder="you@company.com"
          />

          <Text style={styles.label}>Password</Text>
          <PasswordField value={password} onChangeText={setPassword} placeholder="Create a strong password" />
          <Text style={styles.hint}>At least 8 characters.</Text>

          {error && <Text style={styles.error}>{error}</Text>}

          <Pressable style={styles.button} onPress={handleSubmit} disabled={isSubmitting}>
            {isSubmitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Create Account</Text>}
          </Pressable>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  )
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.backgroundLight },
  content: { flexGrow: 1, padding: 24, paddingVertical: 40 },
  backLink: { flexDirection: 'row', alignItems: 'center', gap: 6, marginBottom: 20 },
  backText: { color: colors.textMuted, fontSize: 14, fontWeight: '500' },
  logoWrap: { alignItems: 'center', marginBottom: 20 },
  title: { color: colors.textOnLight, fontSize: 20, fontWeight: '700', textAlign: 'center' },
  subtitle: { color: colors.textMuted, fontSize: 14, textAlign: 'center', marginTop: 4, marginBottom: 16 },
  label: { color: colors.textOnLight, fontSize: 13, fontWeight: '500', marginBottom: 6, marginTop: 12 },
  hint: { color: colors.textMuted, fontSize: 12, marginTop: 4 },
  error: { color: colors.negative, fontSize: 13, marginTop: 12 },
  button: {
    backgroundColor: colors.primary,
    borderRadius: 10,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: 20,
  },
  buttonText: { color: '#fff', fontSize: 15, fontWeight: '600' },
})
