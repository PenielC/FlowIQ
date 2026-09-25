import { api } from './api'
import type { ApiResponse, CompanyLogoResponse } from './types'

export async function fetchCompanyLogo() {
  const res = await api.get<ApiResponse<CompanyLogoResponse>>('/api/companies/logo')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load company logo')
  return res.data.data
}
