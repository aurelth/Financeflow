import { useState } from 'react'
import { ChevronLeft, ChevronRight, TrendingUp, TrendingDown, Wallet, PiggyBank, Loader2 } from 'lucide-react'
import { useDashboardSummary } from '../api/useBudgets'
import { useBudgetSummary } from '../api/useBudgets'
import BudgetCard from '../components/BudgetCard'
import BudgetForm from '../components/BudgetForm'
import DeleteBudgetDialog from '../components/DeleteBudgetDialog'
import type { Budget, BudgetSummary } from '../types/budget.types'

function getCurrentPeriod() {
  const now = new Date()
  return { month: now.getMonth() + 1, year: now.getFullYear() }
}

interface SummaryCardProps {
  title:    string
  value:    number
  icon:     React.ReactNode
  color:    string
}

function SummaryCard({ title, value, icon, color }: SummaryCardProps) {
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
      </div>
    </div>
  )
}

export default function DashboardPage() {
  const [period, setPeriod]                   = useState(getCurrentPeriod)
  const [showForm, setShowForm]               = useState(false)
  const [editingBudget, setEditingBudget]     = useState<Budget | null>(null)
  const [deletingBudget, setDeletingBudget]   = useState<Budget | null>(null)

  const { data: summary,   isLoading: loadingSummary }  = useDashboardSummary(period)
  const { data: summaries = [], isLoading: loadingBudgets } = useBudgetSummary(period)

  const isLoading = loadingSummary || loadingBudgets

  const monthLabel = new Date(period.year, period.month - 1).toLocaleString('pt-BR', {
    month: 'long',
    year:  'numeric',
  })

  const top3Budgets = [...summaries]
    .sort((a, b) => b.percentage - a.percentage)
    .slice(0, 3)

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

  function handleEdit(s: BudgetSummary) {
    setEditingBudget({
      id:            s.id,
      categoryId:    s.categoryId,
      categoryName:  s.categoryName,
      categoryIcon:  s.categoryIcon,
      categoryColor: s.categoryColor,
      month:         s.month,
      year:          s.year,
      limitAmount:   s.limitAmount,
      createdAt:     '',
      updatedAt:     null,
    })
    setShowForm(true)
  }

  function handleCloseForm() {
    setShowForm(false)
    setEditingBudget(null)
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
        <>
          {/* Cards de sumário */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
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
          </div>

          {/* Top 3 orçamentos */}
          <div>
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-slate-200 font-semibold">
                Orçamentos em destaque
              </h2>
              <span className="text-xs text-slate-500">
                Top 3 por % utilizada
              </span>
            </div>

            {top3Budgets.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-12 bg-slate-900 border border-slate-800 rounded-2xl space-y-3">
                <div className="w-12 h-12 bg-slate-800 rounded-2xl flex items-center justify-center">
                  <PiggyBank size={22} className="text-slate-500" />
                </div>
                <p className="text-slate-400 text-sm">
                  Nenhum orçamento definido para {monthLabel}
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
                {top3Budgets.map(s => (
                  <BudgetCard
                    key={s.id}
                    summary={s}
                    onEdit={() => handleEdit(s)}
                    onDelete={() => setDeletingBudget({
                      id:            s.id,
                      categoryId:    s.categoryId,
                      categoryName:  s.categoryName,
                      categoryIcon:  s.categoryIcon,
                      categoryColor: s.categoryColor,
                      month:         s.month,
                      year:          s.year,
                      limitAmount:   s.limitAmount,
                      createdAt:     '',
                      updatedAt:     null,
                    })}
                  />
                ))}
              </div>
            )}
          </div>
        </>
      )}

      {/* Modal de criação/edição */}
      {showForm && (
        <BudgetForm
          budget={editingBudget ?? undefined}
          month={period.month}
          year={period.year}
          onClose={handleCloseForm}
        />
      )}

      {/* Modal de confirmação de exclusão */}
      {deletingBudget && (
        <DeleteBudgetDialog
          budget={deletingBudget}
          onClose={() => setDeletingBudget(null)}
        />
      )}
    </div>
  )
}