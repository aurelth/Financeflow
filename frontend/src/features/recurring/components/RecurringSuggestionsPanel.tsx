import { useState } from 'react'
import { CalendarClock, ChevronDown, ChevronUp, Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { resolveRecurrence, resolveType } from '@/lib/enumUtils'
import { TransactionType } from '@/features/categories/types/category.types'
import { RecurrenceType } from '@/features/transactions/types/transaction.types'
import {
  useConfirmRecurring,
  useDismissRecurring,
  useRecurringSuggestions,
} from '../api/useRecurring'
import type { RecurringSuggestion } from '../types/recurring.types'

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

const CADENCE_KEY: Partial<Record<RecurrenceType, string>> = {
  [RecurrenceType.Weekly]:  'recurring.cadence.weekly',
  [RecurrenceType.Monthly]: 'recurring.cadence.monthly',
  [RecurrenceType.Yearly]:  'recurring.cadence.yearly',
}

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('pt-BR')
}

// Assinaturas e contas recorrentes que o histórico revela e que ainda não são uma série.
// Some quando não há nada a sugerir; começa recolhido para não empurrar a lista de transações.
export default function RecurringSuggestionsPanel() {
  const { t } = useTranslation('transactions')
  const { data, isLoading } = useRecurringSuggestions()
  const confirm = useConfirmRecurring()
  const dismiss = useDismissRecurring()
  const [open, setOpen] = useState(false)

  const items = data?.items ?? []
  if (isLoading || items.length === 0) return null

  const busyKey = confirm.isPending ? confirm.variables?.key : dismiss.isPending ? dismiss.variables?.key : null

  function handleConfirm(item: RecurringSuggestion) {
    confirm.mutate({ type: item.type, key: item.key }, {
      onSuccess: result => toast.success(t('recurring.confirmed', { count: result.projectedCount })),
    })
  }

  return (
    <section
      aria-label={t('recurring.title')}
      className="rounded-xl overflow-hidden"
      style={{ border: '1px solid rgba(16,185,129,0.25)' }}
    >
      <button
        type="button"
        onClick={() => setOpen(o => !o)}
        aria-expanded={open}
        className="w-full flex items-center gap-2 px-4 py-3 text-sm text-left"
        style={{ background: 'rgba(16,185,129,0.08)', color: 'var(--ff-emerald)' }}
      >
        <CalendarClock size={15} />
        <span className="font-medium" data-testid="recurring-summary">
          {t('recurring.summary', { count: items.length })}
        </span>
        {data && data.monthlyExpenseTotal > 0 && (
          <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            {t('recurring.monthlyTotal', { amount: brl.format(data.monthlyExpenseTotal) })}
          </span>
        )}
        <span className="ml-auto">{open ? <ChevronUp size={15} /> : <ChevronDown size={15} />}</span>
      </button>

      {open && (
        <ul className="divide-y" style={{ borderColor: 'var(--ff-border)' }}>
          {items.map(item => {
            const cadenceKey = CADENCE_KEY[resolveRecurrence(item.cadence)]
            const isIncome = resolveType(item.type) === TransactionType.Income
            const busy = busyKey === item.key

            return (
              <li
                key={`${item.type}-${item.key}`}
                data-testid="recurring-item"
                className="flex flex-wrap items-center justify-between gap-3 px-4 py-3"
                style={{ background: 'var(--ff-bg-card)' }}
              >
                <div className="min-w-0">
                  <p className="text-sm truncate" style={{ color: 'var(--ff-text-primary)' }}>
                    {item.description}
                  </p>
                  <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                    {cadenceKey ? t(cadenceKey) : ''} · {t('recurring.occurrences', { count: item.occurrences })} ·{' '}
                    {t('recurring.next', { date: formatDate(item.nextExpectedDate) })}
                    {item.amountVaries && ` · ${t('recurring.varies')}`}
                  </p>
                </div>

                <div className="flex items-center gap-3">
                  <p
                    className="text-sm font-semibold"
                    style={{ color: isIncome ? 'var(--ff-income)' : 'var(--ff-expense)' }}
                  >
                    {item.amountVaries ? '~' : ''}{brl.format(item.amount)}
                  </p>
                  <button
                    onClick={() => dismiss.mutate({ type: item.type, key: item.key })}
                    disabled={busy || confirm.isPending || dismiss.isPending}
                    aria-label={`${t('recurring.dismiss')} ${item.description}`}
                    className="h-8 px-3 rounded-lg text-xs font-medium disabled:opacity-50"
                    style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-muted)' }}
                  >
                    {t('recurring.dismiss')}
                  </button>
                  <button
                    onClick={() => handleConfirm(item)}
                    disabled={busy || confirm.isPending || dismiss.isPending}
                    aria-label={`${t('recurring.confirm')} ${item.description}`}
                    className="flex items-center gap-1.5 h-8 px-3 rounded-lg text-xs font-medium disabled:opacity-50"
                    style={{ background: 'rgba(16,185,129,0.12)', color: 'var(--ff-emerald)', border: '1px solid rgba(16,185,129,0.3)' }}
                  >
                    {busy && confirm.isPending && <Loader2 size={12} className="animate-spin" />}
                    {t('recurring.confirm')}
                  </button>
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
