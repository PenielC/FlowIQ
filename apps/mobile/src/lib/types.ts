export interface UserResponse {
  id: string
  email: string
  firstName: string
  lastName: string
  role: string
  companyId: string
  companyName: string
  companyCurrency: string
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAtUtc: string
  refreshToken: string
  user: UserResponse
}

export interface ApiResponse<T> {
  success: boolean
  data: T | null
  message: string | null
  errors: Record<string, string[]> | null
}

export interface PagedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export const transactionCategories = ['Sales', 'OperatingExpense', 'RentAndLease', 'Payroll', 'Utilities', 'Other'] as const
export type TransactionCategory = (typeof transactionCategories)[number]

export interface TransactionResponse {
  id: string
  description: string
  category: string
  amount: number
  transactionDateUtc: string
  status: string
  currency: string
  amountInReportingCurrency: number
}

export interface DashboardSummaryResponse {
  cashBalance: number
  monthlyRevenue: number
  revenueChangePercent: number | null
  monthlyExpenses: number
  expensesChangePercent: number | null
  recentTransactions: TransactionResponse[]
}

export interface CompanyLogoResponse {
  dataUrl: string | null
}

export interface InvoiceResponse {
  id: string
  customerName: string
  amount: number
  issueDateUtc: string
  dueDateUtc: string
  status: string
  currency: string
  amountInReportingCurrency: number
}

export interface InvoiceSummaryResponse {
  totalOutstanding: number
  customerCount: number
  upcomingInvoices: InvoiceResponse[]
}

export const teamRoles = ['Owner', 'Admin', 'Member'] as const
export type TeamRole = (typeof teamRoles)[number]

export interface TeamMemberResponse {
  id: string
  email: string
  firstName: string
  lastName: string
  role: string
  joinedAtUtc: string
}

export interface InvitationResponse {
  id: string
  email: string
  role: string
  token: string
  status: string
  expiresAtUtc: string
}

export interface CashFlowPointResponse {
  dateUtc: string
  actual: number | null
  forecast: number | null
}

export interface CashFlowForecastResponse {
  points: CashFlowPointResponse[]
}

export interface InsightResponse {
  title: string
  description: string
  tone: string
}

export interface AiInsightsResponse {
  insights: InsightResponse[]
}

export interface SubscriptionPlanResponse {
  key: string
  displayName: string
  monthlyPriceUsd: number
  features: string[]
}

export interface SubscriptionStatusResponse {
  hasSubscription: boolean
  planKey: string | null
  planDisplayName: string | null
  monthlyPriceUsd: number | null
  status: string | null
  currentPeriodEndUtc: string | null
}

export interface SuggestCategoryResponse {
  category: string
  confidence: number
}

export interface ExchangeRateResponse {
  rate: number | null
  isLive: boolean
}
