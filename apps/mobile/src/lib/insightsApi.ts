import { api } from './api'
import type { AiInsightsResponse, ApiResponse } from './types'

export async function fetchAiInsights() {
  const res = await api.get<ApiResponse<AiInsightsResponse>>('/api/insights')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load insights')
  return res.data.data
}
