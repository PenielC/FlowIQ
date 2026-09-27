import { IconTextField } from '@/components/IconTextField'
import { Logo } from '@/components/Logo'
import { colors } from '@/constants/colors'
import { useAuth } from '@/lib/AuthContext'
import { ArrowLeft, Mail } from 'lucide-react-native'
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

export function ForgotPasswordScreen({ onSwitchToLogin }: { onSwitchToLogin: () => void }) {
  const { forgotPassword } = useAuth()
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  async function handleSubmit() {
    setError(null)
    setIsSubmitting(true)
    try {
      await forgotPassword(email)
      setSubmitted(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong')
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

          {submitted ? (
            <>
              <Text style={styles.title}>Check your email</Text>
              <Text style={styles.subtitle}>
                If an account exists for {email}, we&apos;ve sent a link to reset your password. It expires in 1 hour.
              </Text>
              <Pressable style={styles.button} onPress={onSwitchToLogin}>
                <Text style={styles.buttonText}>Back to login</Text>
              </Pressable>
            </>
          ) : (
            <>
              <Text style={styles.title}>Forgot your password?</Text>
              <Text style={styles.subtitle}>Enter your email and we&apos;ll send you a reset link.</Text>

              <Text style={styles.label}>Email Address</Text>
              <IconTextField
                icon={Mail}
                value={email}
                onChangeText={setEmail}
                autoCapitalize="none"
                keyboardType="email-address"
                placeholder="you@company.com"
              />

              {error && <Text style={styles.error}>{error}</Text>}

              <Pressable style={styles.button} onPress={handleSubmit} disabled={isSubmitting}>
                {isSubmitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Send Reset Link</Text>}
              </Pressable>
            </>
          )}
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
