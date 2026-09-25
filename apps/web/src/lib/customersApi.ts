import { api } from './api'
import type { ApiResponse, CustomerResponse, PagedResult } from './types'

export async function fetchCustomers(pageNumber = 1, pageSize = 20) {
  const res = await api.get<ApiResponse<PagedResult<CustomerResponse>>>('/api/customers', {
    params: { pageNumber, pageSize },
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load customers')
  return res.data.data
}

export async function fetchCustomerById(id: string) {
  const res = await api.get<ApiResponse<CustomerResponse>>(`/api/customers/${id}`)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load customer')
  return res.data.data
}

export interface CustomerInput {
  name: string
  email: string | null
  phone: string | null
  notes: string | null
}

export async function createCustomer(input: CustomerInput) {
  const res = await api.post<ApiResponse<CustomerResponse>>('/api/customers', input)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to create customer')
  return res.data.data
}

export async function updateCustomer(id: string, input: CustomerInput) {
  const res = await api.put<ApiResponse<CustomerResponse>>(`/api/customers/${id}`, input)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to update customer')
  return res.data.data
}

export async function deleteCustomer(id: string) {
  const res = await api.delete<ApiResponse<object>>(`/api/customers/${id}`)
  if (!res.data.success) throw new Error(res.data.message ?? 'Failed to delete customer')
}
