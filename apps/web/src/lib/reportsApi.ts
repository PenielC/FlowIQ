import { api } from './api'
import type { ApiResponse, ReportsSummaryResponse } from './types'

export async function fetchReportsSummary(monthsBack = 6) {
  const res = await api.get<ApiResponse<ReportsSummaryResponse>>('/api/reports/summary', { params: { monthsBack } })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load reports summary')
  return res.data.data
}

export async function downloadTransactionsCsv() {
  const res = await api.get('/api/reports/export/transactions', { responseType: 'blob' })
  const url = window.URL.createObjectURL(new Blob([res.data]))
  const link = document.createElement('a')
  link.href = url
  const disposition = res.headers['content-disposition'] as string | undefined
  const match = disposition?.match(/filename="?([^"]+)"?/)
  link.download = match?.[1] ?? 'transactions.csv'
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(url)
}
