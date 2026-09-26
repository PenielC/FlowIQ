import axios from 'axios'
import type { ApiResponse, AuthResponse } from './types'
import { tokenStorage } from './tokenStorage'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5112',
  headers: {
    'Content-Type': 'application/json',
    'X-Client-Platform': 'web',
  },
})

api.interceptors.request.use((config) => {
  const token = tokenStorage.getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = tokenStorage.getRefreshToken()
  if (!refreshToken) return null

  const response = await axios.post<ApiResponse<AuthResponse>>(
    `${api.defaults.baseURL}/api/auth/refresh`,
    { refreshToken },
    { headers: { 'X-Client-Platform': 'web' } },
  )

  const auth = response.data.data
  if (!auth) return null

  tokenStorage.setTokens(auth.accessToken, auth.refreshToken)
  return auth.accessToken
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config
    const isAuthEndpoint = originalRequest?.url?.includes('/api/auth/')

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthEndpoint) {
      originalRequest._retry = true
      try {
        refreshPromise ??= refreshAccessToken().finally(() => {
          refreshPromise = null
        })
        const newToken = await refreshPromise
        if (newToken) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`
          return api(originalRequest)
        }
      } catch {
        // fall through to reject below
      }
      tokenStorage.clear()
      window.location.assign('/login')
    }

    return Promise.reject(error)
  },
)
