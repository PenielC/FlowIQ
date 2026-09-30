import { api } from './api'
import type { ApiResponse, CashFlowForecastResponse, OwnerDrawResponse, OwnerDrawsResponse, SaveOwnerDrawRequest } from './types'

export async function fetchForecast(historyDays = 30, forecastDays = 30) {
  const res = await api.get<ApiResponse<CashFlowForecastResponse>>('/api/forecast', {
    params: { historyDays, forecastDays },
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load forecast')
  return res.data.data
}

/** Planned personal withdrawals, and whether the owner has answered the forecast setup yet. */
export async function fetchOwnerDraws() {
  const res = await api.get<ApiResponse<OwnerDrawsResponse>>('/api/forecast/owner-draws')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load planned withdrawals')
  return res.data.data
}

export async function createOwnerDraw(request: SaveOwnerDrawRequest) {
  const res = await api.post<ApiResponse<OwnerDrawResponse>>('/api/forecast/owner-draws', request)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to save the withdrawal')
  return res.data.data
}

export async function updateOwnerDraw(id: string, request: SaveOwnerDrawRequest) {
  const res = await api.put<ApiResponse<OwnerDrawResponse>>(`/api/forecast/owner-draws/${id}`, request)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to save the withdrawal')
  return res.data.data
}

export async function deleteOwnerDraw(id: string) {
  const res = await api.delete<ApiResponse<object>>(`/api/forecast/owner-draws/${id}`)
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to delete the withdrawal')
}

/** Records that the owner answered the setup (including "I don't take money out"), so the dashboard stops asking. */
export async function completeForecastSetup() {
  const res = await api.post<ApiResponse<object>>('/api/forecast/setup-complete')
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to save')
}
