import { Pencil, Trash2, RefreshCw } from 'lucide-react'
import { cn } from '@/lib/utils'
import { TransactionStatus, RecurrenceType, type Transaction } from '../types/transaction.types'
import { TransactionType } from '../../categories/types/category.types'
import CategoryIcon from '../../categories/components/CategoryIcon'
import AttachmentViewer from './AttachmentViewer'

interface TransactionTableProps {
  transactions: Transaction[]
  onEdit:       (transaction: Transaction) => void
  onDelete:     (transaction: Transaction) => void
}

const statusLabel: Record<TransactionStatus, string> = {
  [TransactionStatus.Paid]:      'Pago',
  [TransactionStatus.Pending]:   'Pendente',
  [TransactionStatus.Scheduled]: 'Agendado',
}

const statusColors: Record<TransactionStatus, string> = {
  [TransactionStatus.Paid]:      'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
  [TransactionStatus.Pending]:   'bg-amber-500/10 text-amber-400 border-amber-500/20',
  [TransactionStatus.Scheduled]: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
}

const recurrenceLabel: Record<RecurrenceType, string> = {
  [RecurrenceType.None]:    '',
  [RecurrenceType.Daily]:   'Diária',
  [RecurrenceType.Weekly]:  'Semanal',
  [RecurrenceType.Monthly]: 'Mensal',
  [RecurrenceType.Yearly]:  'Anual',
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('pt-BR')
}

function formatAmount(amount: number, type: TransactionType) {
  const formatted = new Intl.NumberFormat('pt-BR', {
    style:    'currency',
    currency: 'BRL',
  }).format(amount)
  return type === TransactionType.Income ? `+${formatted}` : `-${formatted}`
}

export default function TransactionTable({
  transactions,
  onEdit,
  onDelete,
}: TransactionTableProps) {
  if (transactions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-slate-500">
        <p className="text-sm">Nenhuma transação encontrada.</p>
      </div>
    )
  }

  return (
    <div className="rounded-xl border border-slate-800 overflow-hidden">
      <table className="w-full text-sm">
        <thead>
          <tr className="bg-slate-900 border-b border-slate-800">
            <th className="text-left px-4 py-3 text-slate-400 font-medium">Descrição</th>
            <th className="text-left px-4 py-3 text-slate-400 font-medium">Categoria</th>
            <th className="text-left px-4 py-3 text-slate-400 font-medium">Data</th>
            <th className="text-left px-4 py-3 text-slate-400 font-medium">Status</th>
            <th className="text-right px-4 py-3 text-slate-400 font-medium">Valor</th>
            <th className="text-right px-4 py-3 text-slate-400 font-medium">Ações</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-800">
          {transactions.map(tx => (
            <tr
              key={tx.id}
              className="bg-slate-900/50 hover:bg-slate-800/50 transition-colors duration-150"
            >
              {/* Descrição */}
              <td className="px-4 py-3">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-slate-200 font-medium truncate max-w-[200px]">
                    {tx.description || '—'}
                  </span>
                  {tx.attachmentPath && (
                    <AttachmentViewer
                      transactionId={tx.id}
                      fileName={tx.attachmentName ?? tx.attachmentPath.split('/').pop() ?? 'comprovante'}
                      triggerIcon="paperclip"
                  />
)}
                  {tx.isRecurring && recurrenceLabel[tx.recurrenceType] && (
                    <span className="flex items-center gap-1 text-xs px-1.5 py-0.5 rounded-md bg-indigo-500/10 text-indigo-400 border border-indigo-500/20 flex-shrink-0">
                      <RefreshCw size={10} />
                      {recurrenceLabel[tx.recurrenceType]}
                    </span>
                  )}
                </div>
                {tx.tags.length > 0 && (
                  <div className="flex gap-1 mt-1 flex-wrap">
                    {tx.tags.map(tag => (
                      <span
                        key={tag}
                        className="text-xs px-1.5 py-0.5 rounded-md bg-slate-800 text-slate-400"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                )}
              </td>

              {/* Categoria */}
              <td className="px-4 py-3">
                <div className="flex items-center gap-2">
                  <span
                    className="w-6 h-6 rounded-md flex items-center justify-center flex-shrink-0"
                    style={{ backgroundColor: `${tx.categoryColor}20` }}
                  >
                    <CategoryIcon icon={tx.categoryIcon} color={tx.categoryColor} size={14} />
                  </span>
                  <div className="min-w-0">
                    <p className="text-slate-300 truncate">{tx.categoryName}</p>
                    {tx.subcategoryName && (
                      <p className="text-xs text-slate-500 truncate">{tx.subcategoryName}</p>
                    )}
                  </div>
                </div>
              </td>

              {/* Data */}
              <td className="px-4 py-3 text-slate-400 whitespace-nowrap">
                {formatDate(tx.date)}
              </td>

              {/* Status */}
              <td className="px-4 py-3">
                <span
                  className={cn(
                    'text-xs px-2 py-0.5 rounded-full border font-medium',
                    statusColors[tx.status]
                  )}
                >
                  {statusLabel[tx.status]}
                </span>
              </td>

              {/* Valor */}
              <td className="px-4 py-3 text-right whitespace-nowrap font-semibold">
                <span className={cn(
                  tx.type === TransactionType.Income ? 'text-emerald-400' : 'text-red-400'
                )}>
                  {formatAmount(tx.amount, tx.type)}
                </span>
              </td>

              {/* Ações */}
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-1">
                  <button
                    onClick={() => onEdit(tx)}
                    className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-all duration-200"
                    title="Editar"
                  >
                    <Pencil size={14} />
                  </button>
                  <button
                    onClick={() => onDelete(tx)}
                    className="p-1.5 text-slate-400 hover:text-red-400 hover:bg-slate-800 rounded-lg transition-all duration-200"
                    title="Remover"
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}