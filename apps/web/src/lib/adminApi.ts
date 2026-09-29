import { api } from './api'
import type { AdminOverviewResponse, ApiResponse, RepairCurrencyDataResponse } from './types'

export async function fetchAdminOverview(
  pageNumber = 1,
  pageSize = 20,
  status?: string,
  year?: number,
  month?: number,
) {
  const res = await api.get<ApiResponse<AdminOverviewResponse>>('/api/admin/overview', {
    params: { pageNumber, pageSize, status, year, month },
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to fetch admin overview')
  return res.data.data
}

export interface UpdateCompanyInput {
  name: string
  currency: string
}

export async function updateCompany(id: string, input: UpdateCompanyInput) {
  const res = await api.put<ApiResponse<object>>(`/api/admin/companies/${id}`, input)
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to update company')
}

export async function activateCompany(id: string) {
  const res = await api.post<ApiResponse<object>>(`/api/admin/companies/${id}/activate`)
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to activate company')
}

export async function deactivateCompany(id: string) {
  const res = await api.post<ApiResponse<object>>(`/api/admin/companies/${id}/deactivate`)
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to deactivate company')
}

/** One-off repair of currency data written before the conversion fixes. Dry run unless told otherwise. */
export async function repairCurrencyData(dryRun: boolean, backfillPaidInvoiceIncome: boolean) {
  const res = await api.post<ApiResponse<RepairCurrencyDataResponse>>('/api/admin/repair-currency-data', {
    dryRun,
    backfillPaidInvoiceIncome,
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to repair currency data')
  return res.data.data
}
