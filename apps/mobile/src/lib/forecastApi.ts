import { api } from './api'
import type { ApiResponse, CashFlowForecastResponse } from './types'

export async function fetchForecast(historyDays = 30, forecastDays = 30) {
  const res = await api.get<ApiResponse<CashFlowForecastResponse>>('/api/forecast', {
    params: { historyDays, forecastDays },
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load forecast')
  return res.data.data
}
