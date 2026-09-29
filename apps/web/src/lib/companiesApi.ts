import { api } from './api'
import type { ApiResponse, CompanyLogoResponse, CurrencyChangePreviewResponse } from './types'

export async function fetchCompanyLogo() {
  const res = await api.get<ApiResponse<CompanyLogoResponse>>('/api/companies/logo')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load company logo')
  return res.data.data
}

export async function uploadCompanyLogo(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  const res = await api.post<ApiResponse<object>>('/api/companies/logo', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to upload logo')
}

export async function removeCompanyLogo() {
  const res = await api.delete<ApiResponse<object>>('/api/companies/logo')
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to remove logo')
}

export async function previewCurrencyChange(currency: string) {
  const res = await api.get<ApiResponse<CurrencyChangePreviewResponse>>('/api/companies/currency/preview', { params: { currency } })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to check the currency change')
  return res.data.data
}

/** Changes the reporting currency; every transaction and invoice is converted into it on the server. */
export async function updateCompanyCurrency(currency: string, manualRates?: Record<string, number>) {
  const res = await api.put<ApiResponse<object>>('/api/companies/currency', { currency, manualRates })
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to update currency')
}
