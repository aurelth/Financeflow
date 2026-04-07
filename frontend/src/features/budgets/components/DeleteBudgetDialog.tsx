import { Trash2 } from 'lucide-react'
import { useDeleteBudget } from '../api/useBudgets'
import type { Budget } from '../types/budget.types'

interface DeleteBudgetDialogProps {
  budget:  Budget
  onClose: () => void
}

export default function DeleteBudgetDialog({ budget, onClose }: DeleteBudgetDialogProps) {
  const deleteBudget = useDeleteBudget()

  const monthName = new Date(budget.year, budget.month - 1).toLocaleString('pt-BR', { month: 'long' })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div
        className="rounded-2xl p-6 w-full max-w-md shadow-2xl"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div
          className="w-12 h-12 rounded-xl flex items-center justify-center mb-4"
          style={{ background: 'rgba(244,63,94,0.1)', border: '1px solid rgba(244,63,94,0.2)' }}
        >
          <Trash2 size={22} style={{ color: 'var(--ff-expense)' }} />
        </div>

        <h2 className="font-semibold text-lg mb-1" style={{ color: 'var(--ff-text-primary)' }}>
          Remover orçamento
        </h2>
        <p className="text-sm mb-6" style={{ color: 'var(--ff-text-muted)' }}>
          Tens a certeza que queres remover o orçamento de{' '}
          <span className="font-medium" style={{ color: 'var(--ff-text-primary)' }}>
            "{budget.categoryName}"
          </span>{' '}
          para{' '}
          <span className="font-medium" style={{ color: 'var(--ff-text-primary)' }}>
            {monthName} de {budget.year}
          </span>
          ? Esta ação não pode ser desfeita.
        </p>

        <div className="flex gap-3">
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
            onClick={() => deleteBudget.mutate(budget.id, { onSuccess: onClose })}
            disabled={deleteBudget.isPending}
            className="flex-1 px-4 py-2.5 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50"
            style={{ background: 'var(--ff-expense)', color: '#fff' }}
            onMouseEnter={e => { if (!deleteBudget.isPending) e.currentTarget.style.background = '#e11d48' }}
            onMouseLeave={e => { if (!deleteBudget.isPending) e.currentTarget.style.background = 'var(--ff-expense)' }}
          >
            {deleteBudget.isPending ? 'Removendo...' : 'Remover'}
          </button>
        </div>
      </div>
    </div>
  )
}