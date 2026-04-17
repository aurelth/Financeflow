import { useState } from 'react'
import { Plus, PiggyBank, ChevronLeft, ChevronRight } from 'lucide-react'
import { useBudgetSummary }      from '../api/useBudgets'
import BudgetCard                from '../components/BudgetCard'
import BudgetForm                from '../components/BudgetForm'
import DeleteBudgetDialog        from '../components/DeleteBudgetDialog'
import EmptyState                from '@/components/ui/EmptyState'
import SkeletonCard              from '@/components/ui/SkeletonCard'
import type { Budget, BudgetSummary } from '../types/budget.types'

function getCurrentPeriod() {
  const now = new Date()
  return { month: now.getMonth() + 1, year: now.getFullYear() }
}

export default function BudgetsPage() {
  const [period, setPeriod]                   = useState(getCurrentPeriod)
  const [showForm, setShowForm]               = useState(false)
  const [editingBudget, setEditingBudget]     = useState<Budget | null>(null)
  const [deletingBudget, setDeletingBudget]   = useState<Budget | null>(null)

  const { data: summaries = [], isLoading } = useBudgetSummary(period)
  const isEmpty = !isLoading && summaries.length === 0

  const monthLabel = new Date(period.year, period.month - 1).toLocaleString('pt-BR', {
    month: 'long', year: 'numeric',
  })

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

  function handleEdit(summary: BudgetSummary) {
    const budget: Budget = {
      id: summary.id, categoryId: summary.categoryId, categoryName: summary.categoryName,
      categoryIcon: summary.categoryIcon, categoryColor: summary.categoryColor,
      month: summary.month, year: summary.year, limitAmount: summary.limitAmount,
      createdAt: '', updatedAt: null,
    }
    setEditingBudget(budget)
    setShowForm(true)
  }

  function toBudget(summary: BudgetSummary): Budget {
    return {
      id: summary.id, categoryId: summary.categoryId, categoryName: summary.categoryName,
      categoryIcon: summary.categoryIcon, categoryColor: summary.categoryColor,
      month: summary.month, year: summary.year, limitAmount: summary.limitAmount,
      createdAt: '', updatedAt: null,
    }
  }

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            Orçamentos
          </h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Defina limites mensais por categoria e acompanhe os seus gastos
          </p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
          onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
        >
          <Plus size={16} />
          Novo orçamento
        </button>
      </div>

      {/* Seletor de mês/ano */}
      <div
        className="flex items-center justify-between rounded-2xl px-5 py-3"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <button
          onClick={handlePrevMonth}
          className="p-1.5 rounded-lg transition-all"
          style={{ color: 'var(--ff-text-muted)' }}
          onMouseEnter={e => {
            e.currentTarget.style.color       = 'var(--ff-text-primary)'
            e.currentTarget.style.background  = 'var(--ff-bg-elevated)'
          }}
          onMouseLeave={e => {
            e.currentTarget.style.color       = 'var(--ff-text-muted)'
            e.currentTarget.style.background  = 'transparent'
          }}
        >
          <ChevronLeft size={18} />
        </button>
        <span className="font-medium capitalize" style={{ color: 'var(--ff-text-primary)' }}>
          {monthLabel}
        </span>
        <button
          onClick={handleNextMonth}
          className="p-1.5 rounded-lg transition-all"
          style={{ color: 'var(--ff-text-muted)' }}
          onMouseEnter={e => {
            e.currentTarget.style.color       = 'var(--ff-text-primary)'
            e.currentTarget.style.background  = 'var(--ff-bg-elevated)'
          }}
          onMouseLeave={e => {
            e.currentTarget.style.color       = 'var(--ff-text-muted)'
            e.currentTarget.style.background  = 'transparent'
          }}
        >
          <ChevronRight size={18} />
        </button>
      </div>

      {/* Skeleton loading em vez de spinner */}
      {isLoading && (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <SkeletonCard key={i} lines={4} height="h-40" />
          ))}
        </div>
      )}

      {/* Grid de cards */}
      {!isLoading && !isEmpty && (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {summaries.map(summary => (
            <BudgetCard
              key={summary.id}
              summary={summary}
              onEdit={() => handleEdit(summary)}
              onDelete={() => setDeletingBudget(toBudget(summary))}
            />
          ))}
        </div>
      )}

      {/* EmptyState reutilizável */}
      {isEmpty && (
        <EmptyState
          icon={PiggyBank}
          title="Nenhum orçamento definido"
          description={`Ainda não tens orçamentos para ${monthLabel}. Define limites por categoria para controlar os teus gastos.`}
          action={{
            label:   'Criar primeiro orçamento',
            onClick: () => setShowForm(true),
          }}
        />
      )}

      {showForm && (
        <BudgetForm
          budget={editingBudget ?? undefined}
          month={period.month}
          year={period.year}
          onClose={() => { setShowForm(false); setEditingBudget(null) }}
        />
      )}

      {deletingBudget && (
        <DeleteBudgetDialog budget={deletingBudget} onClose={() => setDeletingBudget(null)} />
      )}
    </div>
  )
}