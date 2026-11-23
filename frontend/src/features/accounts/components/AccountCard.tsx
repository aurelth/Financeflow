import { Pencil, Archive, RotateCcw, Star, ShieldCheck } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { formatCurrency } from '@/lib/format'
import AccountIcon from './AccountIcon'
import type { Account } from '../types/account.types'

interface AccountCardProps {
  account:   Account
  onEdit:    (account: Account) => void
  onArchive: (account: Account) => void
  onRestore: (account: Account) => void
}

export default function AccountCard({ account, onEdit, onArchive, onRestore }: AccountCardProps) {
  const { t, i18n } = useTranslation('accounts')

  const subtitle = [
    t(`types.${account.type}`),
    account.bankName,
    account.accountLast4 ? t('card.last4', { last4: account.accountLast4 }) : null,
  ].filter(Boolean).join(' · ')

  return (
    <div
      data-testid={`account-card-${account.id}`}
      className="rounded-2xl p-5 flex flex-col gap-4"
      style={{
        background: 'var(--ff-bg-card)',
        border:     '1px solid var(--ff-border)',
        opacity:    account.isArchived ? 0.6 : 1,
      }}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-3 min-w-0">
          <div
            className="w-11 h-11 rounded-xl flex items-center justify-center flex-shrink-0"
            style={{ background: `${account.color}1a` }}
          >
            <AccountIcon type={account.type} color={account.color} />
          </div>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <p className="text-sm font-semibold truncate" style={{ color: 'var(--ff-text-primary)' }}>
                {account.name}
              </p>
              {account.isDefault && (
                <span
                  className="inline-flex items-center gap-1 text-[10px] font-medium px-1.5 py-0.5 rounded-full"
                  style={{ background: 'var(--ff-emerald-subtle)', color: 'var(--ff-emerald)' }}
                >
                  <Star size={10} />
                  {t('card.default')}
                </span>
              )}
              {account.isArchived && (
                <span
                  className="text-[10px] font-medium px-1.5 py-0.5 rounded-full"
                  style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-muted)' }}
                >
                  {t('card.archived')}
                </span>
              )}
            </div>
            <p className="text-xs mt-0.5 truncate" style={{ color: 'var(--ff-text-muted)' }}>
              {subtitle}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-1 flex-shrink-0">
          {!account.isArchived && (
            <button
              onClick={() => onEdit(account)}
              title={t('card.edit')}
              aria-label={`${t('card.edit')} ${account.name}`}
              className="p-2 rounded-lg transition-colors"
              style={{ color: 'var(--ff-text-muted)' }}
              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
            >
              <Pencil size={15} />
            </button>
          )}
          {account.isArchived ? (
            <button
              onClick={() => onRestore(account)}
              title={t('card.restore')}
              aria-label={`${t('card.restore')} ${account.name}`}
              className="p-2 rounded-lg transition-colors"
              style={{ color: 'var(--ff-text-muted)' }}
              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
            >
              <RotateCcw size={15} />
            </button>
          ) : !account.isDefault && (
            <button
              onClick={() => onArchive(account)}
              title={t('card.archive')}
              aria-label={`${t('card.archive')} ${account.name}`}
              className="p-2 rounded-lg transition-colors"
              style={{ color: 'var(--ff-text-muted)' }}
              onMouseEnter={e => (e.currentTarget.style.color = '#f87171')}
              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
            >
              <Archive size={15} />
            </button>
          )}
        </div>
      </div>

      <div>
        <p className="text-xs uppercase tracking-wide" style={{ color: 'var(--ff-text-muted)' }}>
          {t('card.balance')}
        </p>
        <p
          className="text-xl font-semibold"
          style={{ color: account.balance < 0 ? 'var(--ff-expense)' : 'var(--ff-text-primary)' }}
        >
          {formatCurrency(account.balance, i18n.language, account.currency)}
        </p>
      </div>

      <p className="flex items-center gap-1.5 text-xs" style={{ color: 'var(--ff-text-muted)' }}>
        <ShieldCheck size={12} style={{ color: account.hasBankKey ? 'var(--ff-emerald)' : 'var(--ff-text-muted)' }} />
        {account.hasBankKey ? t('card.recognized') : t('card.notRecognized')}
      </p>
    </div>
  )
}
