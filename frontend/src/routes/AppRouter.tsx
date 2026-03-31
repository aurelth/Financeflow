import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import AuthLayout   from '@/components/layout/AuthLayout'
import AppLayout    from '@/components/layout/AppLayout'
import PrivateRoute from './PrivateRoute'
import PublicRoute  from './PublicRoute'
import LoginPage    from '@/features/auth/pages/LoginPage'
import RegisterPage from '@/features/auth/pages/RegisterPage'
import CategoriesPage   from '@/features/categories/pages/CategoriesPage'
import TransactionsPage from '@/features/transactions/pages/TransactionsPage'
import BudgetsPage      from '@/features/budgets/pages/BudgetsPage'
import DashboardPage    from '@/features/dashboard/pages/DashboardPage'
import ComparisonPage from '@/features/dashboard/pages/ComparisonPage'
import ExportsPage from '@/features/reports/pages/ExportsPage'

// Placeholder para fases futuras
const Placeholder = ({ title }: { title: string }) => (
  <div className="flex items-center justify-center h-full">
    <div className="text-center">
      <h2 className="text-2xl font-bold text-white mb-2">{title}</h2>
      <p className="text-slate-400">Em desenvolvimento... ♻️</p>
    </div>
  </div>
)

export default function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>

        {/* Rotas públicas */}
        <Route element={<PublicRoute />}>
          <Route element={<AuthLayout />}>
            <Route path="/login"    element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
          </Route>
        </Route>

        {/* Rotas privadas */}
        <Route element={<PrivateRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/dashboard"    element={<DashboardPage />} />
            <Route path="/transactions" element={<TransactionsPage />} />
            <Route path="/categories"   element={<CategoriesPage />} />
            <Route path="/budgets"      element={<BudgetsPage />} />
            <Route path="/comparison"   element={<ComparisonPage />} />
            <Route path="/reports"      element={<Placeholder title="Relatórios" />} />
            <Route path="/exports"      element={<ExportsPage />} />
            <Route path="/profile"      element={<Placeholder title="Perfil" />} />
            <Route path="/settings"     element={<Placeholder title="Configurações" />} />
          </Route>
        </Route>

        {/* Redirect padrão */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  )
}