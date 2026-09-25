import { isAxiosError } from 'axios'
import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api } from './api'
import { acceptInvitation as acceptInvitationRequest } from './teamApi'
import { tokenStorage } from './tokenStorage'
import type { ApiResponse, AuthResponse, UserResponse } from './types'

function extractErrorMessage(error: unknown): string {
  if (isAxiosError<ApiResponse<unknown>>(error)) {
    const data = error.response?.data
    if (data?.errors) {
      return Object.values(data.errors).flat().join(' ')
    }
    if (data?.message) return data.message
  }
  return 'Something went wrong. Please try again.'
}

interface RegisterInput {
  companyName: string
  firstName: string
  lastName: string
  email: string
  password: string
}

interface AcceptInvitationInput {
  token: string
  firstName: string
  lastName: string
  password: string
}

interface AuthContextValue {
  user: UserResponse | null
  isLoading: boolean
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  register: (input: RegisterInput) => Promise<void>
  acceptInvitation: (input: AcceptInvitationInput) => Promise<void>
  logout: () => void
  refreshUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserResponse | null>(null)
  const [isLoading, setIsLoading] = useState(() => !!tokenStorage.getAccessToken())

  useEffect(() => {
    if (!tokenStorage.getAccessToken()) return

    api
      .get<ApiResponse<UserResponse>>('/api/auth/me')
      .then((res) => setUser(res.data.data))
      .catch(() => tokenStorage.clear())
      .finally(() => setIsLoading(false))
  }, [])

  async function refreshUser() {
    const res = await api.get<ApiResponse<UserResponse>>('/api/auth/me')
    setUser(res.data.data)
  }

  async function applyAuthResponse(auth: AuthResponse) {
    tokenStorage.setTokens(auth.accessToken, auth.refreshToken)
    setUser(auth.user)
  }

  async function login(email: string, password: string) {
    try {
      const res = await api.post<ApiResponse<AuthResponse>>('/api/auth/login', { email, password })
      if (!res.data.data) throw new Error('Login failed')
      await applyAuthResponse(res.data.data)
    } catch (error) {
      throw new Error(extractErrorMessage(error))
    }
  }

  async function register(input: RegisterInput) {
    try {
      const res = await api.post<ApiResponse<AuthResponse>>('/api/auth/register', input)
      if (!res.data.data) throw new Error('Registration failed')
      await applyAuthResponse(res.data.data)
    } catch (error) {
      throw new Error(extractErrorMessage(error))
    }
  }

  async function acceptInvitation(input: AcceptInvitationInput) {
    try {
      const auth = await acceptInvitationRequest(input)
      await applyAuthResponse(auth)
    } catch (error) {
      throw new Error(extractErrorMessage(error))
    }
  }

  function logout() {
    tokenStorage.clear()
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, isAuthenticated: !!user, login, register, acceptInvitation, logout, refreshUser }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
