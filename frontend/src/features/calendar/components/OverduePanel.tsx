import { useState } from 'react'
import { AlertTriangle, ChevronDown, ChevronUp } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import CalendarEntryRow from './CalendarEntryRow'
import type { CalendarEntry, CalendarOverdue } from '../types/calendar.types'

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

interface OverduePanelProps {
  overdue:   CalendarOverdue
  onPay:     (entry: CalendarEntry) => void
  payingId:  string | null
}

// Contas vencidas e ainda em aberto, de qualquer mês: a primeira coisa que o usuário precisa resolver.
// Some quando não há nenhuma.
export default function OverduePanel({ overdue, onPay, payingId }: OverduePanelProps) {
  const { t } = useTranslation('calendar')
  const [open, setOpen] = useState(false)

  if (overdue.count === 0) return null

  return (
    <section
      aria-label={t('overdue.title')}
      className="rounded-xl overflow-hidden"
      style={{ border: '1px solid rgba(244,63,94,0.3)' }}
    >
      <button
        type="button"
        onClick={() => setOpen(o => !o)}
        aria-expanded={open}
        className="w-full flex flex-wrap items-center gap-2 px-4 py-3 text-sm text-left"
        style={{ background: 'rgba(244,63,94,0.08)', color: 'var(--ff-expense)' }}
      >
        <AlertTriangle size={15} />
        <span className="font-medium" data-testid="overdue-summary">
          {t('overdue.summary', { count: overdue.count })}
        </span>
        {overdue.expenseAmount > 0 && (
          <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            {t('overdue.toPay', { amount: brl.format(overdue.expenseAmount) })}
          </span>
        )}
        {overdue.incomeAmount > 0 && (
          <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            {t('overdue.toReceive', { amount: brl.format(overdue.incomeAmount) })}
          </span>
        )}
        <span className="ml-auto">{open ? <ChevronUp size={15} /> : <ChevronDown size={15} />}</span>
      </button>

      {open && (
        <ul className="space-y-2 p-3" style={{ background: 'var(--ff-bg-card)' }}>
          {overdue.entries.map(entry => (
            <CalendarEntryRow
              key={entry.id}
              entry={entry}
              onPay={onPay}
              isPaying={payingId === entry.id}
              showDate
            />
          ))}
        </ul>
      )}
    </section>
  )
}
