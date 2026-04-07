import { Trash2, RefreshCw } from 'lucide-react'
import { useDeleteTransaction } from '../api/useTransactions'
import type { Transaction } from '../types/transaction.types'

interface DeleteTransactionDialogProps {
  transaction: Transaction
  onClose:     () => void
}

export default function DeleteTransactionDialog({ transaction, onClose }: DeleteTransactionDialogProps) {
  const deleteTransaction = useDeleteTransaction()
  const isRecorrente      = !!transaction.recurrenceGroupId

  function handleDelete(deleteFuture: boolean) {
    deleteTransaction.mutate({ id: transaction.id, deleteFuture }, { onSuccess: onClose })
  }

  // Modal de escolha para recorrentes
  if (isRecorrente) {
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
        <div
          className="rounded-2xl p-6 w-full max-w-md shadow-2xl"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <div
            className="w-12 h-12 rounded-xl flex items-center justify-center mb-4"
            style={{ background: 'var(--ff-emerald-subtle)', border: '1px solid rgba(16,185,129,0.2)' }}
          >
            <RefreshCw size={22} style={{ color: 'var(--ff-emerald)' }} />
          </div>

          <h2 className="font-semibold text-lg mb-1" style={{ color: 'var(--ff-text-primary)' }}>
            Remover transação recorrente
          </h2>
          <p className="text-sm mb-6" style={{ color: 'var(--ff-text-muted)' }}>
            <span className="font-medium" style={{ color: 'var(--ff-text-primary)' }}>
              "{transaction.description || 'Esta transação'}"
            </span>{' '}
            faz parte de um grupo recorrente. O que deseja remover?
          </p>

          <div className="flex flex-col gap-2">
            <button
              onClick={() => handleDelete(false)}
              disabled={deleteTransaction.isPending}
              className="w-full px-4 py-3 rounded-xl text-sm font-medium transition-colors text-left disabled:opacity-50"
              style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-primary)' }}
              onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
            >
              <p className="font-semibold">Apenas esta</p>
              <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
                Remove somente esta ocorrência. As futuras permanecem.
              </p>
            </button>
            <button
              onClick={() => handleDelete(true)}
              disabled={deleteTransaction.isPending}
              className="w-full px-4 py-3 rounded-xl text-sm font-medium transition-colors text-left disabled:opacity-50"
              style={{
                border:     '1px solid rgba(244,63,94,0.3)',
                background: 'rgba(244,63,94,0.05)',
              }}
              onMouseEnter={e => (e.currentTarget.style.background = 'rgba(244,63,94,0.1)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'rgba(244,63,94,0.05)')}
            >
              <p className="font-semibold" style={{ color: 'var(--ff-expense)' }}>
                Esta e todas as futuras
              </p>
              <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
                Remove esta ocorrência e todas as próximas do grupo.
              </p>
            </button>
            <button
              onClick={onClose}
              className="w-full px-4 py-2.5 rounded-xl text-sm transition-colors mt-1"
              style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-muted)' }}
              onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
            >
              Cancelar
            </button>
          </div>
        </div>
      </div>
    )
  }

  // Modal padrão para não recorrentes
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
          Remover transação
        </h2>
        <p className="text-sm mb-6" style={{ color: 'var(--ff-text-muted)' }}>
          Tens a certeza que queres remover{' '}
          <span className="font-medium" style={{ color: 'var(--ff-text-primary)' }}>
            "{transaction.description || 'esta transação'}"
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
            onClick={() => handleDelete(false)}
            disabled={deleteTransaction.isPending}
            className="flex-1 px-4 py-2.5 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50"
            style={{ background: 'var(--ff-expense)', color: '#fff' }}
            onMouseEnter={e => (e.currentTarget.style.background = '#e11d48')}
            onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-expense)')}
          >
            {deleteTransaction.isPending ? 'Removendo...' : 'Remover'}
          </button>
        </div>
      </div>
    </div>
  )
}