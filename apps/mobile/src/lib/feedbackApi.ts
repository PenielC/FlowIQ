import { api } from './api'
import type { ApiResponse } from './types'

export async function sendFeedback(message: string) {
  const res = await api.post<ApiResponse<object>>('/api/feedback', { message })
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to send feedback')
}
