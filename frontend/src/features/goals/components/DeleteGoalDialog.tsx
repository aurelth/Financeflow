import { Loader2 } from 'lucide-react'
import type { GoalProgressResultDto } from '../types/goal.types'

interface DeleteGoalDialogProps {
  goal:      GoalProgressResultDto
  isPending: boolean
  onCancel:  () => void
  onConfirm: () => void
}

export default function DeleteGoalDialog({ goal, isPending, onCancel, onConfirm }: DeleteGoalDialogProps) {
  return (
    <div className="space-y-4">
      <p className="text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
        Tens a certeza que queres remover a meta{' '}
        <span className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
          {goal.emoji} {goal.name}
        </span>
        ? Todo o progresso acumulado será perdido.
      </p>

      <div className="flex gap-3">
        <button
          onClick={onCancel}
          className="flex-1 h-10 rounded-xl text-sm font-medium transition-colors"
          style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-secondary)', border: '1px solid var(--ff-border)' }}
          onMouseEnter={e => (e.currentTarget.style.background = '#222222')}
          onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
        >
          Cancelar
        </button>
        <button
          onClick={onConfirm}
          disabled={isPending}
          className="flex-1 h-10 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50"
          style={{ background: 'rgba(244,63,94,0.15)', color: 'var(--ff-expense)', border: '1px solid rgba(244,63,94,0.3)' }}
          onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = 'rgba(244,63,94,0.25)' }}
          onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'rgba(244,63,94,0.15)' }}
        >
          {isPending
            ? <span className="flex items-center justify-center gap-2"><Loader2 size={15} className="animate-spin" />Removendo...</span>
            : 'Remover meta'
          }
        </button>
      </div>
    </div>
  )
}