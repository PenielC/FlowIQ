import { api } from './api'
import type { ApiResponse, SuggestCategoryResponse } from './types'

/** `isIncome` narrows the suggestion to categories that fit money in (true) or out (false). */
export async function suggestCategory(description: string, isIncome?: boolean) {
  const res = await api.post<ApiResponse<SuggestCategoryResponse>>('/api/categorisation/suggest', { description, isIncome })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to suggest category')
  return res.data.data
}
