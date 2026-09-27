import { IconTextField } from '@/components/IconTextField'
import { Logo } from '@/components/Logo'
import { PasswordField } from '@/components/PasswordField'
import { colors } from '@/constants/colors'
import { useAuth } from '@/lib/AuthContext'
import { Mail, Receipt, Sparkles, TrendingUp } from 'lucide-react-native'
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

const features = [
  { icon: Sparkles, label: 'AI-powered cash flow predictions' },
  { icon: TrendingUp, label: 'Automated categorisation' },
  { icon: Receipt, label: 'Faster invoice collections' },
]

export function LoginScreen({
  onSwitchToRegister,
  onSwitchToForgotPassword,
}: {
  onSwitchToRegister: () => void
  onSwitchToForgotPassword: () => void
}) {
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit() {
    setError(null)
    setIsSubmitting(true)
    try {
      await login(email, password)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : 'height'} style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={styles.scroll} keyboardShouldPersistTaps="handled">
          <View style={styles.hero}>
            <Logo source={require('@/assets/images/flowiq-logo-v2-light.png')} aspectRatio={1895 / 756} />
            <Text style={styles.eyebrow}>AI Cash Flow Intelligence Platform</Text>
            <Text style={styles.headline}>
              Smarter Cash Flow. <Text style={styles.headlineAccent}>Stronger Businesses.</Text>
            </Text>

            <View style={styles.featureList}>
              {features.map((feature) => (
                <View key={feature.label} style={styles.featureRow}>
                  <View style={styles.featureIcon}>
                    <feature.icon size={14} color={colors.primary} />
                  </View>
                  <Text style={styles.featureLabel}>{feature.label}</Text>
                </View>
              ))}
            </View>
          </View>

          <View style={styles.card}>
            <Text style={styles.title}>Welcome back</Text>
            <Text style={styles.subtitle}>Sign in to your account to continue.</Text>

            <Text style={styles.label}>Email Address</Text>
            <IconTextField
              icon={Mail}
              value={email}
              onChangeText={setEmail}
              autoCapitalize="none"
              keyboardType="email-address"
              placeholder="you@company.com"
            />

            <View style={styles.labelRow}>
              <Text style={styles.label}>Password</Text>
              <Pressable onPress={onSwitchToForgotPassword} hitSlop={8}>
                <Text style={styles.forgotLink}>Forgot password?</Text>
              </Pressable>
            </View>
            <PasswordField value={password} onChangeText={setPassword} placeholder="Enter your password" />

            {error && <Text style={styles.error}>{error}</Text>}

            <Pressable style={styles.button} onPress={handleSubmit} disabled={isSubmitting}>
              {isSubmitting ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Sign In</Text>}
            </Pressable>

            <Pressable onPress={onSwitchToRegister} style={styles.switchLink}>
              <Text style={styles.switchText}>
                Don&apos;t have an account? <Text style={styles.switchTextBold}>Register</Text>
              </Text>
            </Pressable>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  )
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.navy },
  scroll: { flexGrow: 1 },
  hero: { backgroundColor: colors.navy, paddingHorizontal: 24, paddingTop: 12, paddingBottom: 28 },
  eyebrow: {
    color: colors.primary,
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 1,
    textTransform: 'uppercase',
    marginTop: 20,
  },
  headline: { color: colors.textPrimary, fontSize: 24, fontWeight: '700', marginTop: 8, lineHeight: 30 },
  headlineAccent: { color: colors.primary },
  featureList: { marginTop: 20, gap: 12 },
  featureRow: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  featureIcon: {
    width: 28,
    height: 28,
    borderRadius: 14,
    backgroundColor: 'rgba(255,255,255,0.1)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  featureLabel: { color: colors.textSecondary, fontSize: 13, flexShrink: 1 },
  card: {
    flex: 1,
    backgroundColor: colors.backgroundLight,
    borderTopLeftRadius: 28,
    borderTopRightRadius: 28,
    padding: 24,
    paddingTop: 32,
  },
  title: { color: colors.textOnLight, fontSize: 20, fontWeight: '700' },
  subtitle: { color: colors.textMuted, fontSize: 14, marginTop: 4, marginBottom: 20 },
  label: { color: colors.textOnLight, fontSize: 13, fontWeight: '500', marginBottom: 6, marginTop: 12 },
  labelRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  forgotLink: { color: colors.primary, fontSize: 12, fontWeight: '600' },
  error: { color: colors.negative, fontSize: 13, marginTop: 12 },
  button: {
    backgroundColor: colors.primary,
    borderRadius: 10,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: 20,
  },
  buttonText: { color: '#fff', fontSize: 15, fontWeight: '600' },
  switchLink: { marginTop: 20, alignItems: 'center' },
  switchText: { color: colors.textMuted, fontSize: 13 },
  switchTextBold: { color: colors.primary, fontWeight: '600' },
})
