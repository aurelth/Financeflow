import { AlertTriangle, CheckCircle2, Info } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { formatCurrency, formatDate } from '@/lib/format'
import type { AccountReconciliation } from '../types/account.types'

interface ReconciliationStatusLineProps {
  reconciliation?: AccountReconciliation
  currency:        string
}

// Situação da conta frente ao último extrato OFX confirmado: conciliada, com diferença,
// sem extrato com saldo ou impossível de comparar.
export default function ReconciliationStatusLine({ reconciliation, currency }: ReconciliationStatusLineProps) {
  const { t, i18n } = useTranslation('accounts')

  if (!reconciliation) return null

  const date = reconciliation.statementDate ? formatDate(reconciliation.statementDate, i18n.language) : ''

  if (reconciliation.status === 'Matched') {
    return (
      <p
        data-testid="reconciliation-matched"
        className="flex items-center gap-1.5 text-xs"
        style={{ color: 'var(--ff-emerald)' }}
      >
        <CheckCircle2 size={12} />
        {t('reconciliation.matched', { date })}
      </p>
    )
  }

  if (reconciliation.status === 'Divergent') {
    const difference = reconciliation.difference ?? 0
    const hint = difference > 0 ? t('reconciliation.missingIncome') : t('reconciliation.extraIncome')

    return (
      <div
        data-testid="reconciliation-divergent"
        role="status"
        className="rounded-xl px-3 py-2.5 space-y-1.5"
        style={{ background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.3)' }}
      >
        <p className="flex items-start gap-1.5 text-xs font-medium" style={{ color: 'var(--ff-pending)' }}>
          <AlertTriangle size={13} className="flex-shrink-0 mt-0.5" />
          {t('reconciliation.divergent', {
            amount: formatCurrency(Math.abs(difference), i18n.language, currency),
            date,
          })}
        </p>
        <dl className="text-xs grid grid-cols-2 gap-x-3" style={{ color: 'var(--ff-text-secondary)' }}>
          <dt>{t('reconciliation.bankBalance')}</dt>
          <dd className="text-right">
            {formatCurrency(reconciliation.statementBalance ?? 0, i18n.language, currency)}
          </dd>
          <dt>{t('reconciliation.calculatedBalance')}</dt>
          <dd className="text-right">
            {formatCurrency(reconciliation.calculatedBalance ?? 0, i18n.language, currency)}
          </dd>
        </dl>
        <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>{hint}</p>
      </div>
    )
  }

  const message = reconciliation.status === 'Unavailable'
    ? t('reconciliation.unavailable')
    : t('reconciliation.noStatement')

  return (
    <p
      data-testid="reconciliation-none"
      className="flex items-start gap-1.5 text-xs"
      style={{ color: 'var(--ff-text-muted)' }}
    >
      <Info size={12} className="flex-shrink-0 mt-0.5" />
      {message}
    </p>
  )
}
