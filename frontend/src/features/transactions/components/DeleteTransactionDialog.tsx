import { Trash2 } from 'lucide-react'
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

  function handleDelete() {
    deleteTransaction.mutate(transaction.id, {
      onSuccess: onClose,
    })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 w-full max-w-md shadow-2xl">

        {/* Ícone */}
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
            onClick={handleDelete}
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