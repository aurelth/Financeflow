import { useTranslation } from 'react-i18next'
import CalendarEntryRow from './CalendarEntryRow'
import type { CalendarDay, CalendarEntry } from '../types/calendar.types'

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

interface DayEntriesPanelProps {
  /** yyyy-MM-dd do dia selecionado, ou null */
  selectedKey: string | null
  day:         CalendarDay | undefined
  onPay:       (entry: CalendarEntry) => void
  payingId:    string | null
}

export default function DayEntriesPanel({ selectedKey, day, onPay, payingId }: DayEntriesPanelProps) {
  const { t, i18n } = useTranslation('calendar')

  if (!selectedKey) {
    return (
      <p className="text-sm py-6 text-center" style={{ color: 'var(--ff-text-muted)' }}>
        {t('day.select')}
      </p>
    )
  }

  const title = new Date(`${selectedKey}T12:00:00`).toLocaleDateString(i18n.language || 'pt-BR', {
    weekday: 'long', day: 'numeric', month: 'long',
  })

  return (
    <section
      aria-label={title}
      className="rounded-2xl p-4 space-y-3"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="font-semibold capitalize" style={{ color: 'var(--ff-text-primary)' }}>{title}</h2>
        {day && (
          <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            {day.totals.toPay > 0 && `${t('totals.toPay')}: ${brl.format(day.totals.toPay)}`}
            {day.totals.toPay > 0 && day.totals.toReceive > 0 && ' · '}
            {day.totals.toReceive > 0 && `${t('totals.toReceive')}: ${brl.format(day.totals.toReceive)}`}
          </p>
        )}
      </div>

      {!day || day.entries.length === 0 ? (
        <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>{t('day.empty')}</p>
      ) : (
        <ul className="space-y-2">
          {day.entries.map(entry => (
            <CalendarEntryRow key={entry.id} entry={entry} onPay={onPay} isPaying={payingId === entry.id} />
          ))}
        </ul>
      )}
    </section>
  )
}
