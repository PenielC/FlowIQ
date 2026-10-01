import { api } from './api'
import type { ApiResponse } from './types'

export type ProductUpdateAudience = 'Everyone' | 'OwnersAndAdmins'

export interface ProductUpdate {
  id: string
  title: string
  summary: string
  linkUrl: string | null
  linkLabel: string | null
  audience: ProductUpdateAudience
  showOnDashboard: boolean
  publishedAtUtc: string | null
  isUnread: boolean
}

export interface WhatsNew {
  updates: ProductUpdate[]
  unreadCount: number
  dashboardCards: ProductUpdate[]
}

export interface SaveProductUpdateInput {
  title: string
  summary: string
  linkUrl: string | null
  linkLabel: string | null
  audience: ProductUpdateAudience
  showOnDashboard: boolean
}

/** Tells every What's new widget on the page to reload (after one of them marks seen or dismisses). */
export const WHATS_NEW_CHANGED = 'finflow:whats-new-changed'
const changed = () => window.dispatchEvent(new Event(WHATS_NEW_CHANGED))

export async function fetchWhatsNew() {
  const res = await api.get<ApiResponse<WhatsNew>>('/api/whats-new')
  if (!res.data.data) throw new Error(res.data.message ?? "Failed to load what's new")
  return res.data.data
}

export async function markWhatsNewSeen() {
  await api.post('/api/whats-new/seen')
}

export async function dismissProductUpdate(id: string) {
  await api.post(`/api/whats-new/${id}/dismiss`)
  changed()
}

// ---- platform admin

export async function fetchAllProductUpdates() {
  const res = await api.get<ApiResponse<ProductUpdate[]>>('/api/admin/product-updates')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load updates')
  return res.data.data
}

export async function saveProductUpdate(id: string | null, input: SaveProductUpdateInput) {
  const res = id
    ? await api.put<ApiResponse<ProductUpdate>>(`/api/admin/product-updates/${id}`, input)
    : await api.post<ApiResponse<ProductUpdate>>('/api/admin/product-updates', input)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to save')
  return res.data.data
}

export async function setProductUpdatePublished(id: string, published: boolean) {
  const res = await api.post<ApiResponse<ProductUpdate>>(`/api/admin/product-updates/${id}/published`, { published })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to update')
  return res.data.data
}

export async function deleteProductUpdate(id: string) {
  await api.delete(`/api/admin/product-updates/${id}`)
}
