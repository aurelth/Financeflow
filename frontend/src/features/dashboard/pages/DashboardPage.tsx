import { useState } from 'react'
import {
  ChevronLeft, ChevronRight,
  TrendingUp, TrendingDown, Wallet, CalendarClock,
  Loader2,
} from 'lucide-react'
import {
  useDashboardSummary,
  useBalanceEvolution,
  useExpensesByCategory,
  useWeeklyComparison,
} from '../api/useDashboard'
import { useBudgetSummary } from '@/features/budgets/api/useBudgets'
import { useTransactions } from '@/features/transactions/api/useTransactions'
import LineChartCard from '../components/LineChartCard'
import PieChartCard from '../components/PieChartCard'
import BarChartCard from '../components/BarChartCard'
import RecentTransactionsWidget from '../components/RecentTransactionsWidget'
import TopBudgetsWidget from '../components/TopBudgetsWidget'

function getCurrentPeriod() {
  const now = new Date()
  return { month: now.getMonth() + 1, year: now.getFullYear() }
}

interface SummaryCardProps {
  title:    string
  value:    number
  icon:     React.ReactNode
  color:    string
  subtitle?: string
}

function SummaryCard({ title, value, icon, color, subtitle }: SummaryCardProps) {
  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 flex items-center gap-4">
      <div className={`w-11 h-11 rounded-xl flex items-center justify-center flex-shrink-0 ${color}`}>
        {icon}
      </div>
      <div>
        <p className="text-xs text-slate-400 uppercase tracking-wide mb-0.5">{title}</p>
        <p className="text-lg font-semibold text-slate-100">
          {value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
        </p>
        {subtitle && (
          <p className="text-xs text-slate-500 mt-0.5">{subtitle}</p>
        )}
      </div>
    </div>
  )
}

export default function DashboardPage() {
  const [period, setPeriod] = useState(getCurrentPeriod)

  const monthLabel = new Date(period.year, period.month - 1).toLocaleString('pt-BR', {
    month: 'long',
    year:  'numeric',
  })

  // Filtro de datas para transações recentes
  const dateFrom = new Date(period.year, period.month - 1, 1).toISOString().split('T')[0]
  const dateTo   = new Date(period.year, period.month, 0).toISOString().split('T')[0]

  const { data: summary,            isLoading: l1 } = useDashboardSummary(period)
  const { data: balanceEvolution = [], isLoading: l2 } = useBalanceEvolution(period)
  const { data: expensesByCategory = [], isLoading: l3 } = useExpensesByCategory(period)
  const { data: weeklyComparison = [],   isLoading: l4 } = useWeeklyComparison(period)
  const { data: budgetSummaries = [],    isLoading: l5 } = useBudgetSummary(period)
  const { data: transactionsData,        isLoading: l6 } = useTransactions({
    page:     1,
    pageSize: 5,
    dateFrom,
    dateTo,
  })

  const isLoading       = l1 || l2 || l3 || l4 || l5 || l6
  const recentTx        = transactionsData?.items ?? []

  function handlePrevMonth() {
    setPeriod(p => {
      const date = new Date(p.year, p.month - 2)
      return { month: date.getMonth() + 1, year: date.getFullYear() }
    })
  }

  function handleNextMonth() {
    setPeriod(p => {
      const date = new Date(p.year, p.month)
      return { month: date.getMonth() + 1, year: date.getFullYear() }
    })
  }

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div>
        <h1 className="text-xl font-semibold text-white">Dashboard</h1>
        <p className="text-slate-400 text-sm mt-0.5">
          Visão geral das suas finanças
        </p>
      </div>

      {/* Seletor de mês/ano */}
      <div className="flex items-center justify-between bg-slate-900 border border-slate-800 rounded-2xl px-5 py-3">
        <button
          onClick={handlePrevMonth}
          className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-all"
        >
          <ChevronLeft size={18} />
        </button>
        <span className="text-slate-200 font-medium capitalize">{monthLabel}</span>
        <button
          onClick={handleNextMonth}
          className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-all"
        >
          <ChevronRight size={18} />
        </button>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin text-indigo-400" />
        </div>
      )}

      {!isLoading && (
        <div className="space-y-6">

          {/* Cards de sumário */}
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
            <SummaryCard
              title="Receitas"
              value={summary?.totalIncome ?? 0}
              icon={<TrendingUp size={20} className="text-emerald-400" />}
              color="bg-emerald-500/10 border border-emerald-500/20"
            />
            <SummaryCard
              title="Despesas"
              value={summary?.totalExpenses ?? 0}
              icon={<TrendingDown size={20} className="text-red-400" />}
              color="bg-red-500/10 border border-red-500/20"
            />
            <SummaryCard
              title="Saldo"
              value={summary?.balance ?? 0}
              icon={<Wallet size={20} className="text-indigo-400" />}
              color="bg-indigo-500/10 border border-indigo-500/20"
            />
            <SummaryCard
              title="Saldo Projetado"
              value={summary?.projectedBalance ?? 0}
              icon={<CalendarClock size={20} className="text-violet-400" />}
              color="bg-violet-500/10 border border-violet-500/20"
              subtitle="Inclui transações agendadas"
            />
          </div>

          {/* Gráficos — linha 1 */}
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
            <LineChartCard data={balanceEvolution} />
            <PieChartCard  data={expensesByCategory} />
          </div>

          {/* Gráfico de barras */}
          <BarChartCard data={weeklyComparison} />

          {/* Widgets — linha final */}
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
            <RecentTransactionsWidget transactions={recentTx} />
            <TopBudgetsWidget         summaries={budgetSummaries} />
          </div>

        </div>
      )}
    </div>
  )
}