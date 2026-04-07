import { Trash2, RefreshCw } from 'lucide-react'
import { useDeleteTransaction } from '../api/useTransactions'
import type { Transaction } from '../types/transaction.types'

interface DeleteTransactionDialogProps {
  transaction: Transaction
  onClose:     () => void
}

export default function DeleteTransactionDialog({
  transaction,
  onClose,
}: DeleteTransactionDialogProps) {
  const deleteTransaction = useDeleteTransaction()

  // Controla etapa do modal para recorrentes
  const isRecorrente = !!transaction.recurrenceGroupId

  function handleDelete(deleteFuture: boolean) {
    deleteTransaction.mutate(
      { id: transaction.id, deleteFuture },
      { onSuccess: onClose }
    )
  }

  // Modal de escolha para recorrentes
  if (isRecorrente) {
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 w-full max-w-md shadow-2xl">

          <div className="w-12 h-12 rounded-xl bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-center mb-4">
            <RefreshCw size={22} className="text-indigo-400" />
          </div>

          <h2 className="text-slate-100 font-semibold text-lg mb-1">
            Remover transação recorrente
          </h2>
          <p className="text-slate-400 text-sm mb-6">
            <span className="text-slate-200 font-medium">
              "{transaction.description || 'Esta transação'}"
            </span>{' '}
            faz parte de um grupo recorrente. O que deseja remover?
          </p>

          <div className="flex flex-col gap-2">
            <button
              onClick={() => handleDelete(false)}
              disabled={deleteTransaction.isPending}
              className="w-full px-4 py-3 rounded-xl border border-slate-700 text-slate-200 text-sm font-medium hover:bg-slate-800 transition-colors text-left disabled:opacity-50"
            >
              <p className="font-semibold">Apenas esta</p>
              <p className="text-xs text-slate-400 mt-0.5">
                Remove somente esta ocorrência. As futuras permanecem.
              </p>
            </button>
            <button
              onClick={() => handleDelete(true)}
              disabled={deleteTransaction.isPending}
              className="w-full px-4 py-3 rounded-xl border border-red-500/30 bg-red-500/5 text-sm font-medium hover:bg-red-500/10 transition-colors text-left disabled:opacity-50"
            >
              <p className="font-semibold text-red-400">Esta e todas as futuras</p>
              <p className="text-xs text-slate-400 mt-0.5">
                Remove esta ocorrência e todas as próximas do grupo.
              </p>
            </button>
            <button
              onClick={onClose}
              className="w-full px-4 py-2.5 rounded-xl border border-slate-700 text-slate-400 text-sm hover:bg-slate-800 transition-colors mt-1"
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
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 w-full max-w-md shadow-2xl">

        <div className="w-12 h-12 rounded-xl bg-red-500/10 border border-red-500/20 flex items-center justify-center mb-4">
          <Trash2 size={22} className="text-red-400" />
        </div>

        <h2 className="text-slate-100 font-semibold text-lg mb-1">
          Remover transação
        </h2>
        <p className="text-slate-400 text-sm mb-6">
          Tens a certeza que queres remover{' '}
          <span className="text-slate-200 font-medium">
            "{transaction.description || 'esta transação'}"
          </span>
          ? Esta ação não pode ser desfeita.
        </p>

        <div className="flex gap-3">
          <button
            onClick={onClose}
            className="flex-1 px-4 py-2.5 rounded-xl border border-slate-700 text-slate-300 text-sm font-medium hover:bg-slate-800 transition-colors"
          >
            Cancelar
          </button>
          <button
            onClick={() => handleDelete(false)}
            disabled={deleteTransaction.isPending}
            className="flex-1 px-4 py-2.5 rounded-xl bg-red-500 hover:bg-red-600 disabled:opacity-50 text-white text-sm font-semibold transition-colors"
          >
            {deleteTransaction.isPending ? 'Removendo...' : 'Remover'}
          </button>
        </div>
      </div>
    </div>
  )
}