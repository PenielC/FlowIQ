import { Route, Routes } from 'react-router-dom'
import { AdminRoute } from './components/AdminRoute'
import { AppLayout } from './components/AppLayout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AcceptInvitationPage } from './pages/auth/AcceptInvitationPage'
import { ForgotPasswordPage } from './pages/auth/ForgotPasswordPage'
import { LoginPage } from './pages/auth/LoginPage'
import { RegisterPage } from './pages/auth/RegisterPage'
import { ResetPasswordPage } from './pages/auth/ResetPasswordPage'
import { AdminPage } from './pages/admin/AdminPage'
import { CustomersPage } from './pages/customers/CustomersPage'
import { DashboardPage } from './pages/DashboardPage'
import { ForecastingPage } from './pages/forecasting/ForecastingPage'
import { InvoicesPage } from './pages/invoices/InvoicesPage'
import { LandingPage } from './pages/LandingPage'
import { ReportsPage } from './pages/reports/ReportsPage'
import { SettingsPage } from './pages/settings/SettingsPage'
import { SubscriptionsPage } from './pages/subscriptions/SubscriptionsPage'
import { ImportTransactionsPage } from './pages/transactions/ImportTransactionsPage'
import { TransactionsPage } from './pages/transactions/TransactionsPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/accept-invitation" element={<AcceptInvitationPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="transactions" element={<TransactionsPage />} />
          <Route path="transactions/import" element={<ImportTransactionsPage />} />
          <Route path="invoices" element={<InvoicesPage />} />
          <Route path="forecasting" element={<ForecastingPage />} />
          <Route path="customers" element={<CustomersPage />} />
          <Route path="subscriptions" element={<SubscriptionsPage />} />
          <Route path="reports" element={<ReportsPage />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route element={<AdminRoute />}>
            <Route path="admin" element={<AdminPage />} />
          </Route>
        </Route>
      </Route>
    </Routes>
  )
}

export default App
