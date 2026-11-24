import { useState } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useCalendarMonth, usePayTransaction } from '../api/useCalendar'
import CalendarGrid from '../components/CalendarGrid'
import DayEntriesPanel from '../components/DayEntriesPanel'
import OverduePanel from '../components/OverduePanel'
import { dateKeyOf, shiftMonth } from '../lib/calendarGrid'
import type { CalendarEntry, CalendarTotals } from '../types/calendar.types'

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

const TOTAL_CARDS: { key: keyof CalendarTotals; color: string }[] = [
  { key: 'toPay',     color: 'var(--ff-pending)' },
  { key: 'toReceive', color: 'var(--ff-income)' },
  { key: 'paid',      color: 'var(--ff-expense)' },
  { key: 'received',  color: 'var(--ff-income)' },
]

export default function CalendarPage() {
  const { t, i18n } = useTranslation('calendar')

  const now = new Date()
  const [{ year, month }, setPeriod] = useState({ year: now.getFullYear(), month: now.getMonth() + 1 })
  const [selectedKey, setSelectedKey] = useState<string | null>(null)

  const { data, isLoading, isError } = useCalendarMonth(year, month)
  const pay = usePayTransaction()

  const todayKey = data ? dateKeyOf(data.today) : null
  const monthLabel = new Date(year, month - 1, 1).toLocaleDateString(i18n.language || 'pt-BR', {
    month: 'long', year: 'numeric',
  })

  // Ao abrir o mês corrente, o dia de hoje já vem selecionado
  const effectiveKey = selectedKey ?? (todayKey && todayKey.startsWith(`${year}-${String(month).padStart(2, '0')}`) ? todayKey : null)
  const selectedDay = data?.days.find(d => dateKeyOf(d.date) === effectiveKey)

  function goTo(delta: number) {
    setPeriod(p => shiftMonth(p.year, p.month, delta))
    setSelectedKey(null)
  }

  function goToday() {
    setPeriod({ year: now.getFullYear(), month: now.getMonth() + 1 })
    setSelectedKey(null)
  }

  function handlePay(entry: CalendarEntry) {
    pay.mutate(entry.id, {
      onSuccess: () => toast.success(t('entry.paidToast', { description: entry.description })),
    })
  }

  const payingId = pay.isPending ? (pay.variables ?? null) : null

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>{t('title')}</h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>{t('subtitle')}</p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => goTo(-1)}
            aria-label={t('nav.prev')}
            className="h-9 w-9 flex items-center justify-center rounded-xl"
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
          >
            <ChevronLeft size={16} />
          </button>
          <p
            data-testid="calendar-month-label"
            className="min-w-36 text-center text-sm font-medium capitalize"
            style={{ color: 'var(--ff-text-primary)' }}
          >
            {monthLabel}
          </p>
          <button
            onClick={() => goTo(1)}
            aria-label={t('nav.next')}
            className="h-9 w-9 flex items-center justify-center rounded-xl"
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
          >
            <ChevronRight size={16} />
          </button>
          <button
            onClick={goToday}
            className="h-9 px-3 rounded-xl text-sm font-medium"
            style={{ background: 'rgba(16,185,129,0.1)', color: 'var(--ff-emerald)', border: '1px solid rgba(16,185,129,0.3)' }}
          >
            {t('nav.today')}
          </button>
        </div>
      </div>

      {isError && (
        <p role="alert" className="text-sm" style={{ color: 'var(--ff-expense)' }}>{t('loadError')}</p>
      )}

      {isLoading && (
        <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>{t('loading')}</p>
      )}

      {data && (
        <>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-3" data-testid="calendar-totals">
            {TOTAL_CARDS.map(({ key, color }) => (
              <div
                key={key}
                className="rounded-2xl px-4 py-3"
                style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
              >
                <p className="text-[11px] uppercase tracking-wide" style={{ color: 'var(--ff-text-muted)' }}>
                  {t(`totals.${key}`)}
                </p>
                <p className="text-lg font-semibold" style={{ color }}>{brl.format(data.totals[key])}</p>
              </div>
            ))}
          </div>

          <OverduePanel overdue={data.overdue} onPay={handlePay} payingId={payingId} />

          <div className="grid gap-5 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)] items-start">
            <CalendarGrid
              year={year}
              month={month}
              days={data.days}
              today={dateKeyOf(data.today)}
              selectedKey={effectiveKey}
              onSelect={setSelectedKey}
            />
            <DayEntriesPanel
              selectedKey={effectiveKey}
              day={selectedDay}
              onPay={handlePay}
              payingId={payingId}
            />
          </div>
        </>
      )}
    </div>
  )
}
