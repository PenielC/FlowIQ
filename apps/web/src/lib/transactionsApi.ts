import { api } from './api'
import type { ApiResponse, DashboardSummaryResponse, PagedResult, TransactionResponse } from './types'

export async function fetchDashboardSummary(startDateUtc?: string, endDateUtc?: string) {
  const res = await api.get<ApiResponse<DashboardSummaryResponse>>('/api/transactions/summary', {
    params: startDateUtc && endDateUtc ? { startDateUtc, endDateUtc } : undefined,
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load dashboard summary')
  return res.data.data
}

export async function fetchTransactions(pageNumber = 1, pageSize = 20) {
  const res = await api.get<ApiResponse<PagedResult<TransactionResponse>>>('/api/transactions', {
    params: { pageNumber, pageSize },
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load transactions')
  return res.data.data
}

export interface CreateTransactionInput {
  description: string
  category: string
  amount: number
  transactionDateUtc: string
  status: string
  currency: string
  exchangeRate?: number
}

export async function createTransaction(input: CreateTransactionInput) {
  const res = await api.post<ApiResponse<TransactionResponse>>('/api/transactions', input)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to create transaction')
  return res.data.data
}
