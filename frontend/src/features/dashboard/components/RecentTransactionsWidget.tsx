import { ArrowRight } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus } from '@/features/transactions/types/transaction.types'
import CategoryIcon from '@/features/categories/components/CategoryIcon'
import type { Transaction } from '@/features/transactions/types/transaction.types'

interface RecentTransactionsWidgetProps {
  transactions: Transaction[]
}

const statusColors: Record<TransactionStatus, string> = {
  [TransactionStatus.Paid]:      'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
  [TransactionStatus.Pending]:   'bg-amber-500/10 text-amber-400 border-amber-500/20',
  [TransactionStatus.Scheduled]: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
}

const statusLabel: Record<TransactionStatus, string> = {
  [TransactionStatus.Paid]:      'Pago',
  [TransactionStatus.Pending]:   'Pendente',
  [TransactionStatus.Scheduled]: 'Agendado',
}

function formatAmount(amount: number, type: TransactionType) {
  const formatted = new Intl.NumberFormat('pt-BR', {
    style:    'currency',
    currency: 'BRL',
  }).format(amount)
  return type === TransactionType.Income ? `+${formatted}` : `-${formatted}`
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('pt-BR')
}

export default function RecentTransactionsWidget({ transactions }: RecentTransactionsWidgetProps) {
  const navigate = useNavigate()

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-slate-200 font-semibold text-sm">Últimas Transações</h3>
          <p className="text-slate-500 text-xs mt-0.5">As 5 transações mais recentes</p>
        </div>
        <button
          onClick={() => navigate('/transactions')}
          className="flex items-center gap-1 text-xs text-indigo-400 hover:text-indigo-300 transition-colors"
        >
          Ver todas
          <ArrowRight size={12} />
        </button>
      </div>

      {transactions.length === 0 ? (
        <div className="flex items-center justify-center py-8">
          <p className="text-slate-500 text-sm">Nenhuma transação no período</p>
        </div>
      ) : (
        <div className="space-y-2">
          {transactions.map(tx => (
            <div
              key={tx.id}
              className="flex items-center gap-3 p-3 rounded-xl bg-slate-800/50 hover:bg-slate-800 transition-colors"
            >
              {/* Ícone da categoria */}
              <div
                className="w-9 h-9 rounded-xl flex items-center justify-center flex-shrink-0"
                style={{ backgroundColor: `${tx.categoryColor}20`, border: `1px solid ${tx.categoryColor}30` }}
              >
                <CategoryIcon icon={tx.categoryIcon} color={tx.categoryColor} size={16} />
              </div>

              {/* Descrição e data */}
              <div className="flex-1 min-w-0">
                <p className="text-slate-200 text-sm font-medium truncate">
                  {tx.description || '—'}
                </p>
                <div className="flex items-center gap-2 mt-0.5">
                  <span className="text-xs text-slate-500">{formatDate(tx.date)}</span>
                  <span
                    className={cn(
                      'text-xs px-1.5 py-0.5 rounded-full border font-medium',
                      statusColors[tx.status]
                    )}
                  >
                    {statusLabel[tx.status]}
                  </span>
                </div>
              </div>

              {/* Valor */}
              <span className={cn(
                'text-sm font-semibold flex-shrink-0',
                tx.type === TransactionType.Income ? 'text-emerald-400' : 'text-red-400'
              )}>
                {formatAmount(tx.amount, tx.type)}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}