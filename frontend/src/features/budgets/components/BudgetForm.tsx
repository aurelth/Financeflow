import { useState } from 'react'
import { X } from 'lucide-react'
import { useCategories } from '../../categories/api/useCategories'
import { useCreateBudget, useUpdateBudget } from '../api/useBudgets'
import { TransactionType } from '../../categories/types/category.types'
import CategoryIcon from '../../categories/components/CategoryIcon'
import type { Budget, CreateBudgetRequest } from '../types/budget.types'

interface BudgetFormProps {
  budget?:      Budget
  month:        number
  year:         number
  onClose:      () => void
}

export default function BudgetForm({ budget, month, year, onClose }: BudgetFormProps) {
  const isEditing = !!budget

  const [categoryId,   setCategoryId]   = useState(budget?.categoryId ?? '')
  const [limitAmount,  setLimitAmount]  = useState(budget?.limitAmount ?? 0)

  const { data: categories = [] } = useCategories()
  const expenseCategories = categories.filter(c => c.type === TransactionType.Expense)

  const createBudget = useCreateBudget()
  const updateBudget = useUpdateBudget(budget?.id ?? '')

  const isPending = createBudget.isPending || updateBudget.isPending

  function handleSubmit() {
    if (!categoryId || limitAmount <= 0) return

    if (isEditing) {
      updateBudget.mutate({ limitAmount }, { onSuccess: onClose })
    } else {
      const data: CreateBudgetRequest = { categoryId, month, year, limitAmount }
      createBudget.mutate(data, { onSuccess: onClose })
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-md shadow-2xl">

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-800">
          <h2 className="text-slate-100 font-semibold text-lg">
            {isEditing ? 'Editar orçamento' : 'Novo orçamento'}
          </h2>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-all"
          >
            <X size={18} />
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-5 space-y-4">

          {/* Período (só informativo) */}
          <div className="bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5">
            <span className="text-xs text-slate-400 uppercase tracking-wide">Período</span>
            <p className="text-slate-200 text-sm font-medium mt-0.5">
              {new Date(year, month - 1).toLocaleString('pt-BR', { month: 'long', year: 'numeric' })}
            </p>
          </div>

          {/* Categoria — só na criação */}
          {!isEditing && (
            <div>
              <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-1.5 block">
                Categoria de despesa
              </label>
              <div className="grid grid-cols-2 gap-2 max-h-48 overflow-y-auto pr-1">
                {expenseCategories.map(cat => (
                  <button
                    key={cat.id}
                    onClick={() => setCategoryId(cat.id)}
                    className={`flex items-center gap-2 px-3 py-2.5 rounded-xl border text-sm transition-all ${
                      categoryId === cat.id
                        ? 'border-indigo-500/60 bg-indigo-500/10 text-slate-200'
                        : 'border-slate-700 bg-slate-800 text-slate-400 hover:border-slate-600'
                    }`}
                  >
                    <CategoryIcon icon={cat.icon} color={cat.color} size={16} />
                    <span className="truncate">{cat.name}</span>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Limite */}
          <div>
            <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-1.5 block">
              Limite mensal (R$)
            </label>
            <input
              type="number"
              min="0"
              step="0.01"
              value={limitAmount || ''}
              onChange={e => setLimitAmount(Number(e.target.value))}
              placeholder="0,00"
              className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
            />
          </div>
        </div>

        {/* Footer */}
        <div className="flex gap-3 px-6 py-4 border-t border-slate-800">
          <button
            onClick={onClose}
            className="flex-1 px-4 py-2.5 rounded-xl border border-slate-700 text-slate-300 text-sm font-medium hover:bg-slate-800 transition-colors"
          >
            Cancelar
          </button>
          <button
            onClick={handleSubmit}
            disabled={isPending || !categoryId || limitAmount <= 0}
            className="flex-1 px-4 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-semibold transition-colors"
          >
            {isPending ? 'Salvando...' : isEditing ? 'Atualizar' : 'Criar'}
          </button>
        </div>
      </div>
    </div>
  )
}