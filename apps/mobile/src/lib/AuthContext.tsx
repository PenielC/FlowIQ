import { isAxiosError } from 'axios'
import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api } from './api'
import { tokenStorage } from './tokenStorage'
import type { ApiResponse, AuthResponse, UserResponse } from './types'

interface RegisterInput {
  companyName: string
  firstName: string
  lastName: string
  email: string
  password: string
}

interface AuthContextValue {
  user: UserResponse | null
  isLoading: boolean
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  register: (input: RegisterInput) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

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

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    ;(async () => {
      const token = await tokenStorage.getAccessToken()
      if (!token) {
        setIsLoading(false)
        return
      }

      try {
        const res = await api.get<ApiResponse<UserResponse>>('/api/auth/me')
        setUser(res.data.data)
      } catch {
        await tokenStorage.clear()
      } finally {
        setIsLoading(false)
      }
    })()
  }, [])

  async function applyAuthResponse(auth: AuthResponse) {
    await tokenStorage.setTokens(auth.accessToken, auth.refreshToken)
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

  async function logout() {
    await tokenStorage.clear()
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, isAuthenticated: !!user, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
