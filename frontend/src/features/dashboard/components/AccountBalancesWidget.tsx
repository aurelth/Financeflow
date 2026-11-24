import { AlertTriangle, CheckCircle2 } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { formatCurrency } from '@/lib/format'
import {
  useAccounts,
  useAccountsReconciliation,
} from '@/features/accounts/api/useAccounts'
import AccountIcon from '@/features/accounts/components/AccountIcon'

// Saldo atual de cada conta ativa, com o selo de conciliação com o último extrato.
// Com uma única conta sem extrato com saldo o widget só repetiria o card "Saldo", então não aparece.
export default function AccountBalancesWidget() {
  const { t, i18n } = useTranslation('accounts')
  const { data: accounts = [] } = useAccounts()
  const { data: reconciliations = [] } = useAccountsReconciliation()

  const active = accounts.filter(a => !a.isArchived)
  const statusOf = (accountId: string) => reconciliations.find(r => r.accountId === accountId)
  const hasStatement = active.some(a => {
    const status = statusOf(a.id)?.status
    return status === 'Matched' || status === 'Divergent'
  })

  if (active.length < 2 && !hasStatement) return null

  return (
    <div
      data-testid="account-balances-widget"
      className="rounded-2xl p-5"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
          {t('dashboardWidget.title')}
        </h2>
        <Link
          to="/accounts"
          className="text-xs font-medium"
          style={{ color: 'var(--ff-emerald)' }}
        >
          {t('dashboardWidget.viewAll')}
        </Link>
      </div>

      <ul className="space-y-3">
        {active.map(account => {
          const reconciliation = statusOf(account.id)

          return (
            <li
              key={account.id}
              data-testid={`account-balance-${account.id}`}
              className="flex items-center justify-between gap-3"
            >
              <div className="flex items-center gap-3 min-w-0">
                <div
                  className="w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0"
                  style={{ background: `${account.color}1a` }}
                >
                  <AccountIcon type={account.type} color={account.color} size={16} />
                </div>
                <div className="min-w-0">
                  <p className="text-sm truncate" style={{ color: 'var(--ff-text-primary)' }}>
                    {account.name}
                  </p>
                  {reconciliation?.status === 'Divergent' && (
                    <p
                      className="flex items-center gap-1 text-xs"
                      style={{ color: 'var(--ff-pending)' }}
                    >
                      <AlertTriangle size={11} />
                      {t('dashboardWidget.divergentHint')}:{' '}
                      {formatCurrency(Math.abs(reconciliation.difference ?? 0), i18n.language, account.currency)}
                    </p>
                  )}
                  {reconciliation?.status === 'Matched' && (
                    <p
                      className="flex items-center gap-1 text-xs"
                      style={{ color: 'var(--ff-emerald)' }}
                    >
                      <CheckCircle2 size={11} />
                      {t('reconciliation.title')}
                    </p>
                  )}
                </div>
              </div>
              <p
                className="text-sm font-semibold flex-shrink-0"
                style={{ color: account.balance < 0 ? 'var(--ff-expense)' : 'var(--ff-text-primary)' }}
              >
                {formatCurrency(account.balance, i18n.language, account.currency)}
              </p>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
