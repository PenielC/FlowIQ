import { api } from './api'
import type { ApiResponse, SuggestCategoryResponse } from './types'

export async function suggestCategory(description: string) {
  const res = await api.post<ApiResponse<SuggestCategoryResponse>>('/api/categorisation/suggest', { description })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to suggest category')
  return res.data.data
}
