export interface UserResponse {
  id: string
  email: string
  firstName: string
  lastName: string
  role: string
  companyId: string
  companyName: string
  companyCurrency: string
  isPlatformAdmin: boolean
}

export interface AdminCompanyRowResponse {
  id: string
  name: string
  currency: string
  createdAtUtc: string
  ownerName: string | null
  ownerEmail: string | null
  subscriptionStatus: string
  planKey: string | null
  isActive: boolean
}

export interface AdminMonthlyTrendPointResponse {
  year: number
  month: number
  newCompanies: number
  newSubscriptions: number
}

export interface AdminOverviewResponse {
  totalCompanies: number
  newCompaniesThisMonth: number
  totalSubscriptions: number
  newSubscriptionsThisMonth: number
  usageByPlatform: Record<string, number>
  statusCounts: Record<string, number>
  companies: PagedResult<AdminCompanyRowResponse>
  reportYear: number
  reportMonth: number
  monthlyTrend: AdminMonthlyTrendPointResponse[]
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

export const transactionCategories = [
  'Sales',
  'OperatingExpense',
  'RentAndLease',
  'Payroll',
  'Utilities',
  'Other',
  'OwnerDrawings',
  'OwnerContribution',
] as const
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

export interface CustomerResponse {
  id: string
  name: string
  email: string | null
  phone: string | null
  notes: string | null
  createdAtUtc: string
}

export interface InvoiceLineItemResponse {
  id: string
  description: string
  amount: number
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
  lineItems: InvoiceLineItemResponse[]
  notes: string | null
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

export interface ForecastEventResponse {
  dateUtc: string
  label: string
  /** In the reporting currency; negative is money going out. */
  amount: number
  kind: 'OwnerDraw' | 'InvoiceDue'
}

export interface CashFlowForecastResponse {
  points: CashFlowPointResponse[]
  events: ForecastEventResponse[]
  currentBalance: number
  lowestBalance: number | null
  lowestBalanceDateUtc: string | null
}

export const drawFrequencies = ['Monthly', 'SelectedMonths', 'Weekly', 'Once'] as const
export type DrawFrequency = (typeof drawFrequencies)[number]

export interface OwnerDrawResponse {
  id: string
  name: string
  amount: number
  currency: string
  amountInReportingCurrency: number
  frequency: DrawFrequency
  nextDateUtc: string
  months: number[]
}

export interface OwnerDrawsResponse {
  setupCompleted: boolean
  draws: OwnerDrawResponse[]
}

export interface SaveOwnerDrawRequest {
  name: string
  amount: number
  currency: string
  exchangeRate?: number
  frequency: DrawFrequency
  /** yyyy-mm-dd */
  nextDateUtc: string
  months?: number[]
}

export interface SuggestCategoryResponse {
  category: string
  confidence: number
}

export interface MonthlyTrendPointResponse {
  year: number
  month: number
  revenue: number
  expenses: number
}

export interface CategoryTotalResponse {
  category: string
  total: number
}

export interface InvoiceStatusTotalResponse {
  status: string
  count: number
  totalAmount: number
}

export interface ReportsSummaryResponse {
  monthlyTrend: MonthlyTrendPointResponse[]
  categoryBreakdown: CategoryTotalResponse[]
  invoiceStatusBreakdown: InvoiceStatusTotalResponse[]
}

export interface SubscriptionPlanResponse {
  key: string
  displayName: string
  monthlyPriceUsd: number
  trialDays: number
  features: string[]
}

export interface InsightResponse {
  title: string
  description: string
  tone: string
}

export interface AiInsightsResponse {
  insights: InsightResponse[]
}

export interface ExchangeRateResponse {
  rate: number | null
  isLive: boolean
  /** "Frankfurter" or "ExchangeRate-API"; the latter must be credited wherever its rate is shown. */
  source: string | null
}

export interface CurrencyRateNeedResponse {
  currency: string
  transactionCount: number
  invoiceCount: number
  /** Today's rate for display; null when no source has one and the user must enter it. */
  indicativeRate: number | null
}

export interface CurrencyChangePreviewResponse {
  fromCurrency: string
  toCurrency: string
  transactionCount: number
  invoiceCount: number
  currencies: CurrencyRateNeedResponse[]
}

export interface CompanyRepairRowResponse {
  companyId: string
  companyName: string
  currency: string
  transactionsRestated: number
  invoicesRestated: number
  incomeRecorded: number
  problem: string | null
}

export interface RepairCurrencyDataResponse {
  dryRun: boolean
  companies: CompanyRepairRowResponse[]
  transactionsRestated: number
  invoicesRestated: number
  incomeRecorded: number
}

export interface TransactionImportRowResponse {
  rowNumber: number
  transactionDateUtc: string | null
  description: string
  amount: number | null
  suggestedCategory: string
  isDuplicate: boolean
  parseError: string | null
}

export interface TransactionImportPreviewResponse {
  rows: TransactionImportRowResponse[]
  exchangeRate: number
  importableCount: number
}

export interface ImportTransactionsResponse {
  importedCount: number
}

export interface SubscriptionStatusResponse {
  hasSubscription: boolean
  planKey: string | null
  planDisplayName: string | null
  monthlyPriceUsd: number | null
  status: string | null
  currentPeriodEndUtc: string | null
}
