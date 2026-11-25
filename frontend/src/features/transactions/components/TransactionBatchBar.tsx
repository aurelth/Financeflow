import { Pencil, Trash2, X } from 'lucide-react'

interface TransactionBatchBarProps {
  count:    number
  onEdit:   () => void
  onDelete: () => void
  onClear:  () => void
}

// Barra flutuante que aparece enquanto há lançamentos selecionados na tabela
export default function TransactionBatchBar({ count, onEdit, onDelete, onClear }: TransactionBatchBarProps) {
  return (
    <div
      role="toolbar"
      aria-label="Ações em lote"
      className="fixed inset-x-4 bottom-4 z-40 mx-auto flex max-w-xl items-center gap-2 rounded-2xl p-2 pl-4 shadow-2xl pb-[max(0.5rem,env(safe-area-inset-bottom))]"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <span
        className="min-w-0 flex-1 truncate text-sm font-medium"
        style={{ color: 'var(--ff-text-primary)' }}
        aria-live="polite"
      >
        {count} {count === 1 ? 'selecionada' : 'selecionadas'}
      </span>

      <button
        type="button"
        onClick={onEdit}
        className="flex h-10 items-center gap-2 rounded-xl px-3 text-sm font-medium sm:h-9"
        style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
      >
        <Pencil size={14} />
        Editar
      </button>

      <button
        type="button"
        onClick={onDelete}
        className="flex h-10 items-center gap-2 rounded-xl px-3 text-sm font-medium sm:h-9"
        style={{ background: 'rgba(244,63,94,0.1)', color: 'var(--ff-expense)', border: '1px solid rgba(244,63,94,0.3)' }}
      >
        <Trash2 size={14} />
        {/* Em telas estreitas só o ícone: o texto continua para leitores de tela */}
        <span className="max-[420px]:sr-only">Remover</span>
      </button>

      <button
        type="button"
        onClick={onClear}
        aria-label="Limpar seleção"
        title="Limpar seleção"
        className="flex h-10 w-10 items-center justify-center rounded-xl sm:h-9 sm:w-9"
        style={{ color: 'var(--ff-text-muted)' }}
      >
        <X size={16} />
      </button>
    </div>
  )
}
