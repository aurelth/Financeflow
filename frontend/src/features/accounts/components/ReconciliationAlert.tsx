import { AlertTriangle } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { formatCurrency } from '@/lib/format'
import { useAccounts, useAccountsReconciliation } from '../api/useAccounts'

// Aviso global (dashboard) quando alguma conta ativa não bate com o saldo do último extrato.
// Some sozinho quando tudo está conciliado ou nenhuma conta tem extrato com saldo.
export default function ReconciliationAlert() {
  const { t, i18n } = useTranslation('accounts')
  const { data: accounts = [] } = useAccounts()
  const { data: reconciliations = [] } = useAccountsReconciliation()

  const divergent = reconciliations
    .filter(r => r.status === 'Divergent')
    .map(r => ({ reconciliation: r, account: accounts.find(a => a.id === r.accountId) }))
    .filter((x): x is { reconciliation: typeof x.reconciliation; account: NonNullable<typeof x.account> } =>
      x.account !== undefined)

  if (divergent.length === 0) return null

  return (
    <div
      role="alert"
      data-testid="reconciliation-alert"
      className="rounded-2xl px-5 py-3 flex items-start justify-between gap-3 flex-wrap"
      style={{ background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.3)' }}
    >
      <div className="flex items-start gap-2.5 min-w-0">
        <AlertTriangle size={16} className="flex-shrink-0 mt-0.5" style={{ color: 'var(--ff-pending)' }} />
        <div className="min-w-0">
          <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
            {t('page.reconciliationAlert', { count: divergent.length })}
          </p>
          <ul className="text-xs mt-1 space-y-0.5" style={{ color: 'var(--ff-text-secondary)' }}>
            {divergent.map(({ account, reconciliation }) => (
              <li key={account.id}>
                {account.name}: {formatCurrency(Math.abs(reconciliation.difference ?? 0), i18n.language, account.currency)}
              </li>
            ))}
          </ul>
        </div>
      </div>
      <Link
        to="/accounts"
        className="text-xs font-medium underline underline-offset-2 flex-shrink-0"
        style={{ color: 'var(--ff-pending)' }}
      >
        {t('reconciliation.review')}
      </Link>
    </div>
  )
}
