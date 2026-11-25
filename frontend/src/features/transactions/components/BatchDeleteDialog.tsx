import { Trash2 } from 'lucide-react'
import { TransactionType } from '@/features/categories/types/category.types'
import { resolveType } from '@/lib/enumUtils'
import { useBatchDeleteTransactions } from '../api/useTransactions'
import type { Transaction } from '../types/transaction.types'

interface BatchDeleteDialogProps {
  transactions: Transaction[]
  onClose:      () => void
  onDone:       () => void
}

export default function BatchDeleteDialog({ transactions, onClose, onDone }: BatchDeleteDialogProps) {
  const batchDelete = useBatchDeleteTransactions()

  const count         = transactions.length
  const hasSeries     = transactions.some(tx => tx.recurrenceGroupId || tx.installmentGroupId)
  const hasLinkedPair = transactions.some(tx => resolveType(tx.type) === TransactionType.Transfer && tx.transferLinkId)

  function handleDelete() {
    batchDelete.mutate(
      transactions.map(tx => tx.id),
      { onSuccess: () => { onDone(); onClose() } },
    )
  }

  return (
    <div className="fixed inset-0 z-50 flex justify-center p-4 !m-0 overflow-y-auto [align-items:safe_center] bg-black/70 backdrop-blur-sm">
      <div
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="batch-delete-title"
        className="w-full max-w-md rounded-2xl p-6 shadow-2xl"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div
          className="mb-4 flex h-12 w-12 items-center justify-center rounded-xl"
          style={{ background: 'rgba(244,63,94,0.1)', border: '1px solid rgba(244,63,94,0.2)' }}
        >
          <Trash2 size={22} style={{ color: 'var(--ff-expense)' }} />
        </div>

        <h2 id="batch-delete-title" className="mb-1 text-lg font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
          Remover {count} {count === 1 ? 'transação' : 'transações'}
        </h2>
        <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
          Esta ação não pode ser desfeita.
        </p>

        {(hasSeries || hasLinkedPair) && (
          <ul className="mt-3 list-disc space-y-1 pl-5 text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            {hasSeries && (
              <li>Só as selecionadas saem: as demais ocorrências de recorrências e parcelas continuam.</li>
            )}
            {hasLinkedPair && (
              <li>O vínculo das transferências selecionadas é desfeito; a outra ponta fica sem par.</li>
            )}
          </ul>
        )}

        <div className="mt-6 flex gap-3">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 rounded-xl px-4 py-2.5 text-sm font-medium"
            style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={handleDelete}
            disabled={batchDelete.isPending}
            className="flex-1 rounded-xl px-4 py-2.5 text-sm font-semibold disabled:opacity-50"
            style={{ background: 'var(--ff-expense)', color: '#fff' }}
          >
            {batchDelete.isPending ? 'Removendo...' : 'Remover'}
          </button>
        </div>
      </div>
    </div>
  )
}
