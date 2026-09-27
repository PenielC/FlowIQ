import { AppTabBar } from '@/components/AppTabBar'
import { colors } from '@/constants/colors'
import { AuthProvider, useAuth } from '@/lib/AuthContext'
import { ForgotPasswordScreen } from '@/screens/ForgotPasswordScreen'
import { LoginScreen } from '@/screens/LoginScreen'
import { RegisterScreen } from '@/screens/RegisterScreen'
import { StatusBar } from 'expo-status-bar'
import { useState } from 'react'
import { ActivityIndicator, View } from 'react-native'
import { GestureHandlerRootView } from 'react-native-gesture-handler'
import { SafeAreaProvider } from 'react-native-safe-area-context'

function RootNavigator() {
  const { isAuthenticated, isLoading } = useAuth()
  const [authMode, setAuthMode] = useState<'login' | 'register' | 'forgot-password'>('login')

  if (isLoading) {
    return (
      <View style={{ flex: 1, backgroundColor: colors.navy, alignItems: 'center', justifyContent: 'center' }}>
        <ActivityIndicator color={colors.primary} />
      </View>
    )
  }

  if (!isAuthenticated) {
    if (authMode === 'register') {
      return <RegisterScreen onSwitchToLogin={() => setAuthMode('login')} />
    }
    if (authMode === 'forgot-password') {
      return <ForgotPasswordScreen onSwitchToLogin={() => setAuthMode('login')} />
    }
    return (
      <LoginScreen onSwitchToRegister={() => setAuthMode('register')} onSwitchToForgotPassword={() => setAuthMode('forgot-password')} />
    )
  }

  return <AppTabBar />
}

export default function RootLayout() {
  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <AuthProvider>
          <StatusBar style="light" />
          <RootNavigator />
        </AuthProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  )
}
