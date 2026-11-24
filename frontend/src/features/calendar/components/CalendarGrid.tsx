import { useTranslation } from 'react-i18next'
import { resolveStatus, resolveType } from '@/lib/enumUtils'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus } from '@/features/transactions/types/transaction.types'
import { buildMonthGrid, dateKeyOf } from '../lib/calendarGrid'
import type { CalendarDay } from '../types/calendar.types'

const WEEKDAY_KEYS = ['sun', 'mon', 'tue', 'wed', 'thu', 'fri', 'sat'] as const

interface CalendarGridProps {
  year:        number
  month:       number
  days:        CalendarDay[]
  /** yyyy-MM-dd */
  today:       string
  selectedKey: string | null
  onSelect:    (key: string) => void
}

type Marker = 'overdue' | 'toPay' | 'toReceive' | 'settled'

// Um ponto por situação presente no dia, em ordem de urgência
function markersOf(day: CalendarDay | undefined, todayKey: string, key: string): Marker[] {
  if (!day) return []

  const found = new Set<Marker>()
  for (const entry of day.entries) {
    const paid   = resolveStatus(entry.status) === TransactionStatus.Paid
    const income = resolveType(entry.type) === TransactionType.Income

    if (paid)                   found.add('settled')
    else if (key < todayKey)    found.add('overdue')
    else                        found.add(income ? 'toReceive' : 'toPay')
  }

  return (['overdue', 'toPay', 'toReceive', 'settled'] as Marker[]).filter(m => found.has(m))
}

const MARKER_COLOR: Record<Marker, string> = {
  overdue:   'var(--ff-expense)',
  toPay:     'var(--ff-pending)',
  toReceive: 'var(--ff-income)',
  settled:   'var(--ff-text-muted)',
}

export default function CalendarGrid({ year, month, days, today, selectedKey, onSelect }: CalendarGridProps) {
  const { t } = useTranslation('calendar')
  const weeks = buildMonthGrid(year, month)
  const byKey = new Map(days.map(d => [dateKeyOf(d.date), d]))

  return (
    <div
      role="grid"
      aria-label={t('grid.label')}
      className="rounded-2xl p-3 sm:p-4"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div role="row" className="grid grid-cols-7 gap-1 mb-1">
        {WEEKDAY_KEYS.map(key => (
          <div
            key={key}
            role="columnheader"
            className="text-center text-[11px] font-medium uppercase tracking-wide py-1"
            style={{ color: 'var(--ff-text-muted)' }}
          >
            {t(`weekdays.${key}`)}
          </div>
        ))}
      </div>

      {weeks.map((week, weekIndex) => (
        <div key={weekIndex} role="row" className="grid grid-cols-7 gap-1 mb-1">
          {week.map((cell, index) => {
            if (!cell) return <div key={index} role="gridcell" aria-hidden className="h-14 sm:h-20" />

            const day     = byKey.get(cell.key)
            const markers = markersOf(day, today, cell.key)
            const isToday = cell.key === today
            const isSelected = cell.key === selectedKey
            const open = day ? day.totals.toPay + day.totals.toReceive : 0

            return (
              <button
                key={cell.key}
                role="gridcell"
                data-testid={`calendar-day-${cell.key}`}
                aria-selected={isSelected}
                aria-label={t('grid.dayLabel', { day: cell.day, count: day?.entries.length ?? 0 })}
                onClick={() => onSelect(cell.key)}
                className="h-14 sm:h-20 rounded-xl p-1.5 text-left flex flex-col justify-between transition-colors"
                style={{
                  background: isSelected ? 'var(--ff-emerald-subtle)' : 'var(--ff-bg-elevated)',
                  border: `1px solid ${isSelected ? 'var(--ff-emerald)' : isToday ? 'var(--ff-text-muted)' : 'var(--ff-border)'}`,
                }}
              >
                <span
                  className="text-xs font-medium"
                  style={{ color: isToday ? 'var(--ff-emerald)' : 'var(--ff-text-secondary)' }}
                >
                  {cell.day}
                </span>

                <span className="flex items-center gap-1">
                  {markers.map(marker => (
                    <span
                      key={marker}
                      data-testid={`marker-${marker}`}
                      className="h-1.5 w-1.5 rounded-full"
                      style={{ background: MARKER_COLOR[marker] }}
                    />
                  ))}
                </span>

                {open > 0 && (
                  <span className="hidden sm:block text-[10px] truncate" style={{ color: 'var(--ff-text-muted)' }}>
                    {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(open)}
                  </span>
                )}
              </button>
            )
          })}
        </div>
      ))}

      <ul className="flex flex-wrap gap-x-4 gap-y-1 mt-3 text-[11px]" style={{ color: 'var(--ff-text-muted)' }} aria-label={t('legend.label')}>
        {(['overdue', 'toPay', 'toReceive', 'settled'] as Marker[]).map(marker => (
          <li key={marker} className="flex items-center gap-1.5">
            <span className="h-1.5 w-1.5 rounded-full" style={{ background: MARKER_COLOR[marker] }} />
            {t(`legend.${marker}`)}
          </li>
        ))}
      </ul>
    </div>
  )
}
