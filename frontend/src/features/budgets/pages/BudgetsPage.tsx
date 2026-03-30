import { useState } from 'react'
import { Plus, PiggyBank, Loader2, ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useBudgetSummary } from '../api/useBudgets'
import BudgetCard from '../components/BudgetCard'
import BudgetForm from '../components/BudgetForm'
import DeleteBudgetDialog from '../components/DeleteBudgetDialog'
import type { Budget, BudgetSummary } from '../types/budget.types'

function getCurrentPeriod() {
  const now = new Date()
  return { month: now.getMonth() + 1, year: now.getFullYear() }
}

export default function BudgetsPage() {
  const [period, setPeriod]           = useState(getCurrentPeriod)
  const [showForm, setShowForm]       = useState(false)
  const [editingBudget, setEditingBudget]   = useState<Budget | null>(null)
  const [deletingBudget, setDeletingBudget] = useState<Budget | null>(null)

  const { data: summaries = [], isLoading } = useBudgetSummary(period)

  const isEmpty = !isLoading && summaries.length === 0

  const monthLabel = new Date(period.year, period.month - 1).toLocaleString('pt-BR', {
    month: 'long',
    year:  'numeric',
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
    // Converte BudgetSummary para Budget para o formulário
    const budget: Budget = {
      id:            summary.id,
      categoryId:    summary.categoryId,
      categoryName:  summary.categoryName,
      categoryIcon:  summary.categoryIcon,
      categoryColor: summary.categoryColor,
      month:         summary.month,
      year:          summary.year,
      limitAmount:   summary.limitAmount,
      createdAt:     '',
      updatedAt:     null,
    }
    setEditingBudget(budget)
    setShowForm(true)
  }

  function handleCloseForm() {
    setShowForm(false)
    setEditingBudget(null)
  }

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-white">Orçamentos</h1>
          <p className="text-slate-400 text-sm mt-0.5">
            Defina limites mensais por categoria e acompanhe os seus gastos
          </p>
        </div>
        <Button
          onClick={() => setShowForm(true)}
          className="bg-indigo-600 hover:bg-indigo-500 text-white h-9 px-4 gap-2"
        >
          <Plus size={16} />
          Novo orçamento
        </Button>
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

      {/* Grid de cards */}
      {!isLoading && !isEmpty && (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          {summaries.map(summary => (
            <BudgetCard
              key={summary.id}
              summary={summary}
              onEdit={() => handleEdit(summary)}
              onDelete={() => setDeletingBudget({
                id:            summary.id,
                categoryId:    summary.categoryId,
                categoryName:  summary.categoryName,
                categoryIcon:  summary.categoryIcon,
                categoryColor: summary.categoryColor,
                month:         summary.month,
                year:          summary.year,
                limitAmount:   summary.limitAmount,
                createdAt:     '',
                updatedAt:     null,
              })}
            />
          ))}
        </div>
      )}

      {/* Estado vazio */}
      {isEmpty && (
        <div className="flex flex-col items-center justify-center py-20 space-y-3">
          <div className="w-14 h-14 bg-slate-800 rounded-2xl flex items-center justify-center">
            <PiggyBank size={24} className="text-slate-500" />
          </div>
          <p className="text-slate-400 text-sm">
            Nenhum orçamento definido para {monthLabel}
          </p>
          <Button
            onClick={() => setShowForm(true)}
            className="bg-indigo-600 hover:bg-indigo-500 text-white h-9 px-4 gap-2"
          >
            <Plus size={15} />
            Criar primeiro orçamento
          </Button>
        </div>
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