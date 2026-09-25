import { api } from './api'
import type { ApiResponse, ImportTransactionsResponse, TransactionImportPreviewResponse } from './types'

export interface CsvColumnMapping {
  dateColumnIndex: number
  descriptionColumnIndex: number
  amountColumnIndex: number
  dateFormat: string
  hasHeaderRow: boolean
}

export async function previewTransactionImport(
  file: File,
  mapping: CsvColumnMapping,
  currency: string,
  exchangeRate?: number,
) {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('dateColumnIndex', String(mapping.dateColumnIndex))
  formData.append('descriptionColumnIndex', String(mapping.descriptionColumnIndex))
  formData.append('amountColumnIndex', String(mapping.amountColumnIndex))
  formData.append('dateFormat', mapping.dateFormat)
  formData.append('hasHeaderRow', String(mapping.hasHeaderRow))
  formData.append('currency', currency)
  if (exchangeRate !== undefined) formData.append('exchangeRate', String(exchangeRate))

  const res = await api.post<ApiResponse<TransactionImportPreviewResponse>>('/api/transactions/import/preview', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to preview import')
  return res.data.data
}

export interface ImportRowInput {
  transactionDateUtc: string
  description: string
  amount: number
  category: string
}

export async function confirmTransactionImport(rows: ImportRowInput[], currency: string, exchangeRate: number) {
  const res = await api.post<ApiResponse<ImportTransactionsResponse>>('/api/transactions/import/confirm', {
    rows,
    currency,
    exchangeRate,
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to import transactions')
  return res.data.data
}
