import { api } from './api'
import type { ApiResponse, ExchangeRateResponse } from './types'

export async function fetchExchangeRate(from: string, to: string) {
  const res = await api.get<ApiResponse<ExchangeRateResponse>>('/api/exchange-rates', { params: { from, to } })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to fetch exchange rate')
  return res.data.data
}
