import { CheckCircle2, CreditCard, Loader2, RefreshCw } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { resolveStatus, resolveType } from '@/lib/enumUtils'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus } from '@/features/transactions/types/transaction.types'
import type { CalendarEntry } from '../types/calendar.types'

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

interface CalendarEntryRowProps {
  entry:      CalendarEntry
  onPay:      (entry: CalendarEntry) => void
  isPaying:   boolean
  /** Mostra a data (usado na lista de vencidas, que mistura dias). */
  showDate?:  boolean
}

export default function CalendarEntryRow({ entry, onPay, isPaying, showDate = false }: CalendarEntryRowProps) {
  const { t } = useTranslation('calendar')
  const isIncome = resolveType(entry.type) === TransactionType.Income
  const isPaid   = resolveStatus(entry.status) === TransactionStatus.Paid

  return (
    <li
      data-testid="calendar-entry"
      className="flex flex-wrap items-center justify-between gap-3 rounded-xl px-3 py-2.5"
      style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex items-center gap-3 min-w-0">
        <span
          aria-hidden
          className="flex h-8 w-8 items-center justify-center rounded-lg text-base flex-shrink-0"
          style={{ background: `${entry.categoryColor}22` }}
        >
          {entry.categoryIcon}
        </span>
        <div className="min-w-0">
          <p className="flex items-center gap-1.5 text-sm font-medium truncate" style={{ color: 'var(--ff-text-primary)' }}>
            <span className="truncate">{entry.description}</span>
            {entry.installmentNumber && entry.installmentCount && (
              <span
                className="flex items-center gap-1 text-[11px] px-1.5 py-0.5 rounded-md flex-shrink-0"
                style={{ background: 'rgba(99,102,241,0.1)', color: '#818cf8' }}
              >
                <CreditCard size={10} />
                {entry.installmentNumber}/{entry.installmentCount}
              </span>
            )}
            {entry.isRecurring && (
              <span
                title={t('entry.recurring')}
                className="flex-shrink-0"
                style={{ color: 'var(--ff-emerald)' }}
              >
                <RefreshCw size={11} aria-label={t('entry.recurring')} />
              </span>
            )}
          </p>
          <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            {showDate && `${new Date(entry.date.slice(0, 10) + 'T12:00:00').toLocaleDateString('pt-BR')} · `}
            {entry.categoryName}{entry.accountName ? ` · ${entry.accountName}` : ''}
          </p>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <div className="text-right">
          <p className="text-sm font-semibold" style={{ color: isIncome ? 'var(--ff-income)' : 'var(--ff-expense)' }}>
            {isIncome ? '+' : '−'}{brl.format(entry.amount)}
          </p>
          <p
            data-testid="calendar-entry-status"
            className="text-[11px]"
            style={{ color: entry.isOverdue ? 'var(--ff-expense)' : 'var(--ff-text-muted)' }}
          >
            {isPaid
              ? t(isIncome ? 'entry.received' : 'entry.paid')
              : entry.isOverdue ? t('entry.overdue') : t('entry.open')}
          </p>
        </div>

        {isPaid ? (
          <CheckCircle2 size={18} aria-hidden style={{ color: 'var(--ff-income)' }} />
        ) : (
          <button
            onClick={() => onPay(entry)}
            disabled={isPaying}
            aria-label={`${t(isIncome ? 'entry.receive' : 'entry.pay')} ${entry.description}`}
            className="flex items-center gap-1.5 h-8 px-3 rounded-lg text-xs font-medium disabled:opacity-50"
            style={{ background: 'rgba(16,185,129,0.12)', color: 'var(--ff-emerald)', border: '1px solid rgba(16,185,129,0.3)' }}
          >
            {isPaying && <Loader2 size={12} className="animate-spin" />}
            {t(isIncome ? 'entry.receive' : 'entry.pay')}
          </button>
        )}
      </div>
    </li>
  )
}
