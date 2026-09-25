import { api } from './api'
import type { ApiResponse, InvoiceResponse, InvoiceSummaryResponse, PagedResult } from './types'

export async function fetchInvoiceSummary() {
  const res = await api.get<ApiResponse<InvoiceSummaryResponse>>('/api/invoices/summary')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load invoice summary')
  return res.data.data
}

export async function fetchInvoiceById(id: string) {
  const res = await api.get<ApiResponse<InvoiceResponse>>(`/api/invoices/${id}`)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load invoice')
  return res.data.data
}

export async function fetchInvoices(pageNumber = 1, pageSize = 20) {
  const res = await api.get<ApiResponse<PagedResult<InvoiceResponse>>>('/api/invoices', {
    params: { pageNumber, pageSize },
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load invoices')
  return res.data.data
}

export interface CreateInvoiceInput {
  customerName: string
  lineItems: { description: string; amount: number }[]
  issueDateUtc: string
  dueDateUtc: string
  currency: string
  exchangeRate?: number
  notes?: string
}

export async function createInvoice(input: CreateInvoiceInput) {
  const res = await api.post<ApiResponse<InvoiceResponse>>('/api/invoices', input)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to create invoice')
  return res.data.data
}

export async function markInvoicePaid(id: string) {
  const res = await api.post<ApiResponse<InvoiceResponse>>(`/api/invoices/${id}/mark-paid`)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to mark invoice as paid')
  return res.data.data
}
