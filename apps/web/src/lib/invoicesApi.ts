import { api } from './api'
import type {
  ApiResponse,
  InvoiceEmailResponse,
  InvoiceResponse,
  InvoiceSummaryResponse,
  PagedResult,
  PublicInvoiceResponse,
  ReminderRunResponse,
  ReminderSettingsResponse,
} from './types'

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
  /** Where to email the invoice and its reminders. Left out: the saved customer's email, if any. */
  customerEmail?: string
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

/** Emails the invoice to the customer. `toEmail` (optional) is also saved as the invoice's customer email. */
export async function sendInvoiceEmail(id: string, toEmail?: string, message?: string) {
  const res = await api.post<ApiResponse<InvoiceEmailResponse>>(`/api/invoices/${id}/send`, { toEmail, message })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to send the invoice')
  return res.data.data
}

export async function updateInvoiceDelivery(id: string, customerEmail: string | null, remindersPaused: boolean) {
  const res = await api.put<ApiResponse<InvoiceResponse>>(`/api/invoices/${id}/delivery`, { customerEmail, remindersPaused })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to save')
  return res.data.data
}

export async function fetchInvoiceEmails(id: string) {
  const res = await api.get<ApiResponse<InvoiceEmailResponse[]>>(`/api/invoices/${id}/emails`)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load email history')
  return res.data.data
}

export async function fetchReminderSettings() {
  const res = await api.get<ApiResponse<ReminderSettingsResponse>>('/api/invoices/reminder-settings')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load reminder settings')
  return res.data.data
}

export async function saveReminderSettings(enabled: boolean, days: number[]) {
  const res = await api.put<ApiResponse<ReminderSettingsResponse>>('/api/invoices/reminder-settings', { enabled, days })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to save reminder settings')
  return res.data.data
}

/** Sends any reminders that are due right now, instead of waiting for the next scheduled run. */
export async function runRemindersNow() {
  const res = await api.post<ApiResponse<ReminderRunResponse>>('/api/invoices/reminders/run')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to send reminders')
  return res.data.data
}

/** The customer's view of an invoice, from the link in their email. No sign-in. */
export async function fetchPublicInvoice(token: string) {
  const res = await api.get<ApiResponse<PublicInvoiceResponse>>(`/api/public/invoices/${encodeURIComponent(token)}`)
  if (!res.data.data) throw new Error(res.data.message ?? 'This invoice link is not valid.')
  return res.data.data
}
