import { ArrowRight } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus } from '@/features/transactions/types/transaction.types'
import CategoryIcon from '@/features/categories/components/CategoryIcon'
import type { Transaction } from '@/features/transactions/types/transaction.types'

interface RecentTransactionsWidgetProps {
  transactions: Transaction[]
}

const statusStyles: Record<TransactionStatus, { bg: string; color: string; border: string; label: string }> = {
  [TransactionStatus.Paid]: {
    bg:     'rgba(16, 185, 129, 0.1)',
    color:  'var(--ff-income)',
    border: 'rgba(16, 185, 129, 0.2)',
    label:  'Pago',
  },
  [TransactionStatus.Pending]: {
    bg:     'rgba(245, 158, 11, 0.1)',
    color:  'var(--ff-pending)',
    border: 'rgba(245, 158, 11, 0.2)',
    label:  'Pendente',
  },
  [TransactionStatus.Scheduled]: {
    bg:     'rgba(99, 102, 241, 0.1)',
    color:  'var(--ff-scheduled)',
    border: 'rgba(99, 102, 241, 0.2)',
    label:  'Agendado',
  },
}

// Normaliza status que pode chegar como string ou número
function resolveStatus(status: TransactionStatus | string): TransactionStatus {
  if (typeof status === 'number') return status as TransactionStatus
  const map: Record<string, TransactionStatus> = {
    'Paid':      TransactionStatus.Paid,
    'Pending':   TransactionStatus.Pending,
    'Scheduled': TransactionStatus.Scheduled,
  }
  return map[status] ?? TransactionStatus.Paid
}

function formatAmount(amount: number, type: TransactionType) {
  const formatted = new Intl.NumberFormat('pt-BR', {
    style: 'currency', currency: 'BRL',
  }).format(amount)
  return type === TransactionType.Income ? `+${formatted}` : `-${formatted}`
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('pt-BR')
}

// Normaliza type que pode chegar como string ou número
function resolveType(type: TransactionType | string): TransactionType {
  if (typeof type === 'number') return type as TransactionType
  const map: Record<string, TransactionType> = {
    'Income':  TransactionType.Income,
    'Expense': TransactionType.Expense,
  }
  return map[type] ?? TransactionType.Expense
}

export default function RecentTransactionsWidget({ transactions }: RecentTransactionsWidgetProps) {
  const navigate = useNavigate()

  return (
    <div
      className="rounded-2xl p-5"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
            Últimas Transações
          </h3>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            As 5 transações mais recentes
          </p>
        </div>
        <button
          onClick={() => navigate('/transactions')}
          className="flex items-center gap-1 text-xs transition-colors"
          style={{ color: 'var(--ff-emerald)' }}
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald-hover)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
        >
          Ver todas
          <ArrowRight size={12} />
        </button>
      </div>

      {transactions.length === 0 ? (
        <div className="flex items-center justify-center py-8">
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Nenhuma transação no período
          </p>
        </div>
      ) : (
        <div className="space-y-2">
          {transactions.map(tx => {
            // Normaliza status antes de aceder ao mapa de estilos
            const status = resolveStatus(tx.status)
            const s      = statusStyles[status] ?? statusStyles[TransactionStatus.Paid]

            return (
              <div
                key={tx.id}
                className="flex items-center gap-3 p-3 rounded-xl transition-colors"
                style={{ background: 'var(--ff-bg-elevated)' }}
                onMouseEnter={e => (e.currentTarget.style.background = '#222222')}
                onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
              >
                <div
                  className="w-9 h-9 rounded-xl flex items-center justify-center flex-shrink-0"
                  style={{
                    backgroundColor: tx.categoryColor ? `${tx.categoryColor}20` : 'var(--ff-bg-elevated)',
                    border:          tx.categoryColor ? `1px solid ${tx.categoryColor}30` : '1px solid var(--ff-border)',
                  }}
                >
                  <CategoryIcon
                    icon={tx.categoryIcon  ?? 'ellipsis'}
                    color={tx.categoryColor ?? 'var(--ff-text-muted)'}
                    size={16}
                  />
                </div>

                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate" style={{ color: 'var(--ff-text-primary)' }}>
                    {tx.description || '—'}
                  </p>
                  <div className="flex items-center gap-2 mt-0.5">
                    <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                      {formatDate(tx.date)}
                    </span>
                    <span
                      className="text-xs px-1.5 py-0.5 rounded-full font-medium"
                      style={{ background: s.bg, color: s.color, border: `1px solid ${s.border}` }}
                    >
                      {s.label}
                    </span>
                  </div>
                </div>

                <span
                  className="text-sm font-semibold flex-shrink-0"
                  style={{ color: resolveType(tx.type) === TransactionType.Income ? 'var(--ff-income)' : 'var(--ff-expense)' }}
                >
                  {formatAmount(tx.amount, resolveType(tx.type))}
                </span>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}