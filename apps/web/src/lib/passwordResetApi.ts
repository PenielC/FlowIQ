import { api } from './api'
import type { ApiResponse } from './types'

export async function requestPasswordReset(email: string) {
  const res = await api.post<ApiResponse<object>>('/api/auth/forgot-password', { email })
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to request password reset')
}

export async function resetPassword(input: { token: string; newPassword: string }) {
  const res = await api.post<ApiResponse<object>>('/api/auth/reset-password', {
    token: input.token,
    newPassword: input.newPassword,
  })
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to reset password')
}
