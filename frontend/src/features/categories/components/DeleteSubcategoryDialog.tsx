import { Loader2, TriangleAlert } from 'lucide-react'
import type { Subcategory } from '../types/category.types'

interface DeleteSubcategoryDialogProps {
  subcategory: Subcategory
  isPending:   boolean
  onConfirm:   () => void
  onCancel:    () => void
}

export default function DeleteSubcategoryDialog({ subcategory, isPending, onConfirm, onCancel }: DeleteSubcategoryDialogProps) {
  return (
    <div className="space-y-5">
      <div
        className="flex gap-3 p-4 rounded-xl"
        style={{ background: 'rgba(244,63,94,0.08)', border: '1px solid rgba(244,63,94,0.2)' }}
      >
        <TriangleAlert size={18} className="flex-shrink-0 mt-0.5" style={{ color: 'var(--ff-expense)' }} />
        <div className="space-y-1">
          <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
            Remover{' '}
            <span className="font-semibold">"{subcategory.name}"</span>?
          </p>
          <p className="text-xs leading-relaxed" style={{ color: 'var(--ff-text-muted)' }}>
            Se houver transações vinculadas, a subcategoria será desativada e preservada
            no histórico. Caso contrário, será removida permanentemente.
          </p>
        </div>
      </div>

      <div className="flex gap-3">
        <button
          type="button"
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
          style={{ background: 'var(--ff-expense)', color: '#fff' }}
          onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = '#e11d48' }}
          onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-expense)' }}
        >
          {isPending
            ? <span className="flex items-center justify-center gap-2"><Loader2 size={15} className="animate-spin" />Removendo...</span>
            : 'Sim, remover'
          }
        </button>
      </div>
    </div>
  )
}