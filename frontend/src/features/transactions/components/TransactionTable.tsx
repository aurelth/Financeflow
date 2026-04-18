import { Pencil, Trash2, RefreshCw, ArrowLeftRight } from 'lucide-react'
import { TransactionStatus, RecurrenceType, type Transaction } from '../types/transaction.types'
import { TransactionType } from '../../categories/types/category.types'
import CategoryIcon  from '../../categories/components/CategoryIcon'
import AttachmentViewer from './AttachmentViewer'
import { resolveType, resolveStatus } from '@/lib/enumUtils'

interface TransactionTableProps {
  transactions: Transaction[]
  onEdit:       (transaction: Transaction) => void
  onDelete:     (transaction: Transaction) => void
}

const statusConfig: Record<TransactionStatus, { label: string; bg: string; color: string; border: string }> = {
  [TransactionStatus.Paid]: {
    label:  'Pago',
    bg:     'rgba(16,185,129,0.1)',
    color:  'var(--ff-income)',
    border: 'rgba(16,185,129,0.2)',
  },
  [TransactionStatus.Pending]: {
    label:  'Pendente',
    bg:     'rgba(245,158,11,0.1)',
    color:  'var(--ff-pending)',
    border: 'rgba(245,158,11,0.2)',
  },
  [TransactionStatus.Scheduled]: {
    label:  'Agendado',
    bg:     'rgba(99,102,241,0.1)',
    color:  'var(--ff-scheduled)',
    border: 'rgba(99,102,241,0.2)',
  },
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
  const formatted = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(amount)
  if (type === TransactionType.Income)   return `+${formatted}`
  if (type === TransactionType.Transfer) return `↔ ${formatted}`
  return `-${formatted}`
}

// Adicionado: cor do valor por tipo
function getAmountColor(type: TransactionType): string {
  if (type === TransactionType.Income)   return 'var(--ff-income)'
  if (type === TransactionType.Transfer) return '#818cf8'
  return 'var(--ff-expense)'
}

export default function TransactionTable({ transactions, onEdit, onDelete }: TransactionTableProps) {
  if (transactions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
          Nenhuma transação encontrada.
        </p>
      </div>
    )
  }

  return (
    <div
      className="rounded-xl overflow-hidden"
      style={{ border: '1px solid var(--ff-border)' }}
    >
      <table className="w-full text-sm">
        <thead>
          <tr style={{ background: 'var(--ff-bg-card)', borderBottom: '1px solid var(--ff-border)' }}>
            {['Descrição', 'Categoria', 'Data', 'Status', 'Valor', 'Ações'].map((h, i) => (
              <th
                key={h}
                className={`px-4 py-3 font-medium text-xs uppercase tracking-wide ${i >= 4 ? 'text-right' : 'text-left'}`}
                style={{ color: 'var(--ff-text-muted)' }}
              >
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {transactions.map(tx => {
            const type   = resolveType(tx.type)
            const status = resolveStatus(tx.status)
            const s      = statusConfig[status] ?? statusConfig[TransactionStatus.Paid]

            return (
              <tr
                key={tx.id}
                className="transition-colors duration-150"
                style={{ background: 'var(--ff-bg-card)', borderBottom: '1px solid var(--ff-border-subtle)' }}
                onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
                onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-bg-card)')}
              >
                {/* Descrição */}
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span
                      className="font-medium truncate max-w-[200px]"
                      style={{ color: 'var(--ff-text-primary)' }}
                    >
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
                      <span
                        className="flex items-center gap-1 text-xs px-1.5 py-0.5 rounded-md flex-shrink-0"
                        style={{
                          background: 'var(--ff-emerald-subtle)',
                          color:      'var(--ff-emerald)',
                          border:     '1px solid rgba(16,185,129,0.2)',
                        }}
                      >
                        <RefreshCw size={10} />
                        {recurrenceLabel[tx.recurrenceType]}
                      </span>
                    )}
                    {/* Badge Transfer */}
                    {type === TransactionType.Transfer && (
                      <span
                        className="flex items-center gap-1 text-xs px-1.5 py-0.5 rounded-md flex-shrink-0"
                        style={{
                          background: 'rgba(99,102,241,0.1)',
                          color:      '#818cf8',
                          border:     '1px solid rgba(99,102,241,0.2)',
                        }}
                      >
                        <ArrowLeftRight size={10} />
                        Transferência
                      </span>
                    )}
                  </div>
                  {tx.tags.length > 0 && (
                    <div className="flex gap-1 mt-1 flex-wrap">
                      {tx.tags.map(tag => (
                        <span
                          key={tag}
                          className="text-xs px-1.5 py-0.5 rounded-md"
                          style={{
                            background: 'var(--ff-bg-elevated)',
                            color:      'var(--ff-text-muted)',
                          }}
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
                    {/* Ícone especial para Transfer */}
                    {type === TransactionType.Transfer ? (
                      <span
                        className="w-6 h-6 rounded-md flex items-center justify-center flex-shrink-0"
                        style={{ background: 'rgba(99,102,241,0.1)' }}
                      >
                        <ArrowLeftRight size={12} style={{ color: '#818cf8' }} />
                      </span>
                    ) : (
                      <span
                        className="w-6 h-6 rounded-md flex items-center justify-center flex-shrink-0"
                        style={{
                          backgroundColor: tx.categoryColor ? `${tx.categoryColor}20` : 'var(--ff-bg-elevated)',
                        }}
                      >
                        <CategoryIcon
                          icon={tx.categoryIcon  ?? 'ellipsis'}
                          color={tx.categoryColor ?? 'var(--ff-text-muted)'}
                          size={14}
                        />
                      </span>
                    )}
                    <div className="min-w-0">
                      <p className="truncate" style={{ color: 'var(--ff-text-secondary)' }}>
                        {tx.categoryName ?? '—'}
                      </p>
                      {tx.subcategoryName && (
                        <p className="text-xs truncate" style={{ color: 'var(--ff-text-muted)' }}>
                          {tx.subcategoryName}
                        </p>
                      )}
                    </div>
                  </div>
                </td>

                {/* Data */}
                <td className="px-4 py-3 whitespace-nowrap" style={{ color: 'var(--ff-text-muted)' }}>
                  {formatDate(tx.date)}
                </td>

                {/* Status */}
                <td className="px-4 py-3">
                  {type === TransactionType.Transfer ? (                    
                    <span
                      className="text-xs px-2 py-0.5 rounded-full font-medium"
                      style={{ background: 'rgba(99,102,241,0.1)', color: '#818cf8', border: '1px solid rgba(99,102,241,0.2)' }}
                    >
                      Transferência
                    </span>
                  ) : (
                    <span
                      className="text-xs px-2 py-0.5 rounded-full font-medium"
                      style={{ background: s.bg, color: s.color, border: `1px solid ${s.border}` }}
                    >
                      {s.label}
                    </span>
                  )}
                </td>

                {/* Valor */}
                <td className="px-4 py-3 text-right whitespace-nowrap font-semibold">
                  <span style={{ color: getAmountColor(type) }}>
                    {formatAmount(tx.amount, type)}
                  </span>
                </td>

                {/* Ações */}
                <td className="px-4 py-3">
                  <div className="flex items-center justify-end gap-1">
                    <button
                      onClick={() => onEdit(tx)}
                      className="p-1.5 rounded-lg transition-all duration-200"
                      style={{ color: 'var(--ff-text-muted)' }}
                      onMouseEnter={e => {
                        e.currentTarget.style.color      = 'var(--ff-text-primary)'
                        e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                      }}
                      onMouseLeave={e => {
                        e.currentTarget.style.color      = 'var(--ff-text-muted)'
                        e.currentTarget.style.background = 'transparent'
                      }}
                      title="Editar"
                    >
                      <Pencil size={14} />
                    </button>
                    <button
                      onClick={() => onDelete(tx)}
                      className="p-1.5 rounded-lg transition-all duration-200"
                      style={{ color: 'var(--ff-text-muted)' }}
                      onMouseEnter={e => {
                        e.currentTarget.style.color      = 'var(--ff-expense)'
                        e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                      }}
                      onMouseLeave={e => {
                        e.currentTarget.style.color      = 'var(--ff-text-muted)'
                        e.currentTarget.style.background = 'transparent'
                      }}
                      title="Remover"
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}