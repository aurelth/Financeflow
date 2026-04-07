import { useState } from 'react'
import { X } from 'lucide-react'
import { useCategories } from '../../categories/api/useCategories'
import { useCreateBudget, useUpdateBudget } from '../api/useBudgets'
import { TransactionType } from '../../categories/types/category.types'
import CategoryIcon from '../../categories/components/CategoryIcon'
import type { Budget, CreateBudgetRequest } from '../types/budget.types'

interface BudgetFormProps {
  budget?:  Budget
  month:    number
  year:     number
  onClose:  () => void
}

const inputStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  borderRadius: '12px',
  padding:      '10px 16px',
  color:        'var(--ff-text-primary)',
  fontSize:     '14px',
  outline:      'none',
  transition:   'border-color 0.15s',
}

export default function BudgetForm({ budget, month, year, onClose }: BudgetFormProps) {
  const isEditing = !!budget

  const [categoryId,  setCategoryId]  = useState(budget?.categoryId  ?? '')
  const [limitAmount, setLimitAmount] = useState(budget?.limitAmount ?? 0)

  const { data: categories = [] } = useCategories()
  const expenseCategories = categories.filter(c => c.type === TransactionType.Expense)

  const createBudget = useCreateBudget()
  const updateBudget = useUpdateBudget(budget?.id ?? '')
  const isPending    = createBudget.isPending || updateBudget.isPending

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
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div
        className="w-full max-w-md rounded-2xl shadow-2xl"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        {/* Header */}
        <div
          className="flex items-center justify-between px-6 py-4"
          style={{ borderBottom: '1px solid var(--ff-border)' }}
        >
          <h2 className="font-semibold text-lg" style={{ color: 'var(--ff-text-primary)' }}>
            {isEditing ? 'Editar orçamento' : 'Novo orçamento'}
          </h2>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg transition-all"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => {
              e.currentTarget.style.color = 'var(--ff-text-primary)'
              e.currentTarget.style.background = 'var(--ff-bg-elevated)'
            }}
            onMouseLeave={e => {
              e.currentTarget.style.color = 'var(--ff-text-muted)'
              e.currentTarget.style.background = 'transparent'
            }}
          >
            <X size={18} />
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-5 space-y-4">

          {/* Período */}
          <div
            className="rounded-xl px-4 py-2.5"
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
          >
            <span
              className="text-xs uppercase tracking-wide"
              style={{ color: 'var(--ff-text-muted)' }}
            >
              Período
            </span>
            <p className="text-sm font-medium mt-0.5" style={{ color: 'var(--ff-text-primary)' }}>
              {new Date(year, month - 1).toLocaleString('pt-BR', { month: 'long', year: 'numeric' })}
            </p>
          </div>

          {/* Categoria — só na criação */}
          {!isEditing && (
            <div>
              <label
                className="text-xs font-medium uppercase tracking-wide mb-1.5 block"
                style={{ color: 'var(--ff-text-muted)' }}
              >
                Categoria de despesa
              </label>
              <div className="grid grid-cols-2 gap-2 max-h-48 overflow-y-auto pr-1">
                {expenseCategories.map(cat => (
                  <button
                    key={cat.id}
                    onClick={() => setCategoryId(cat.id)}
                    className="flex items-center gap-2 px-3 py-2.5 rounded-xl text-sm transition-all"
                    style={categoryId === cat.id
                      ? { border: '1px solid rgba(16,185,129,0.5)', background: 'var(--ff-emerald-subtle)', color: 'var(--ff-text-primary)' }
                      : { border: '1px solid var(--ff-border)', background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-muted)' }
                    }
                    onMouseEnter={e => { if (categoryId !== cat.id) e.currentTarget.style.borderColor = '#333333' }}
                    onMouseLeave={e => { if (categoryId !== cat.id) e.currentTarget.style.borderColor = 'var(--ff-border)' }}
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
            <label
              className="text-xs font-medium uppercase tracking-wide mb-1.5 block"
              style={{ color: 'var(--ff-text-muted)' }}
            >
              Limite mensal (R$)
            </label>
            <input
              type="number"
              min="0"
              step="0.01"
              value={limitAmount || ''}
              onChange={e => setLimitAmount(Number(e.target.value))}
              placeholder="0,00"
              style={inputStyle}
              onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
              onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
            />
          </div>
        </div>

        {/* Footer */}
        <div
          className="flex gap-3 px-6 py-4"
          style={{ borderTop: '1px solid var(--ff-border)' }}
        >
          <button
            onClick={onClose}
            className="flex-1 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors"
            style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
            onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
            onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
          >
            Cancelar
          </button>
          <button
            onClick={handleSubmit}
            disabled={isPending || !categoryId || limitAmount <= 0}
            className="flex-1 px-4 py-2.5 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50"
            style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
            onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)' }}
            onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald)' }}
          >
            {isPending ? 'Salvando...' : isEditing ? 'Atualizar' : 'Criar'}
          </button>
        </div>
      </div>
    </div>
  )
}