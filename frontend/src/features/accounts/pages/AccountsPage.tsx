import { useState } from 'react'
import { Plus, Landmark } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import EmptyState from '@/components/ui/EmptyState'
import { formatCurrency } from '@/lib/format'
import { useAccounts, useRestoreAccount } from '../api/useAccounts'
import AccountCard from '../components/AccountCard'
import AccountForm from '../components/AccountForm'
import ArchiveAccountDialog from '../components/ArchiveAccountDialog'
import type { Account } from '../types/account.types'

export default function AccountsPage() {
  const { t, i18n } = useTranslation('accounts')
  const [showArchived, setShowArchived] = useState(false)
  const [showForm, setShowForm]         = useState(false)
  const [editing, setEditing]           = useState<Account | null>(null)
  const [archiving, setArchiving]       = useState<Account | null>(null)

  const { data: accounts = [], isLoading } = useAccounts(showArchived)
  const restoreAccount = useRestoreAccount()

  const active = accounts.filter(a => !a.isArchived)
  // Só soma contas na mesma moeda da principal — somar BRL com USD seria enganoso
  const mainCurrency = active.find(a => a.isDefault)?.currency ?? active[0]?.currency ?? 'BRL'
  const totalBalance = active
    .filter(a => a.currency === mainCurrency)
    .reduce((sum, a) => sum + a.balance, 0)

  function handleCloseForm() {
    setShowForm(false)
    setEditing(null)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <div>
          <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            {t('page.title')}
          </h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            {t('page.subtitle')}
          </p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
        >
          <Plus size={16} />
          {t('page.new')}
        </button>
      </div>

      {/* Resumo */}
      <div
        className="rounded-2xl px-5 py-4 flex items-center justify-between gap-3 flex-wrap"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div>
          <p className="text-xs uppercase tracking-wide" style={{ color: 'var(--ff-text-muted)' }}>
            {t('page.totalBalance')}
          </p>
          <p className="text-2xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            {formatCurrency(totalBalance, i18n.language, mainCurrency)}
          </p>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            {t('page.activeAccounts', { count: active.length })}
          </p>
        </div>
        <label className="flex items-center gap-2 text-sm cursor-pointer" style={{ color: 'var(--ff-text-secondary)' }}>
          <input
            type="checkbox"
            checked={showArchived}
            onChange={e => setShowArchived(e.target.checked)}
            style={{ accentColor: 'var(--ff-emerald)', width: 16, height: 16 }}
          />
          {t('page.showArchived')}
        </label>
      </div>

      {!isLoading && accounts.length === 0 && (
        <EmptyState
          icon={Landmark}
          title={t('empty.title')}
          description={t('empty.description')}
          action={{ label: t('page.new'), onClick: () => setShowForm(true) }}
        />
      )}

      {accounts.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {accounts.map(account => (
            <AccountCard
              key={account.id}
              account={account}
              onEdit={a => { setEditing(a); setShowForm(true) }}
              onArchive={setArchiving}
              onRestore={a => restoreAccount.mutate(a, { onSuccess: () => toast.success(t('toast.restored')) })}
            />
          ))}
        </div>
      )}

      {showForm && <AccountForm account={editing ?? undefined} onClose={handleCloseForm} />}
      {archiving && <ArchiveAccountDialog account={archiving} onClose={() => setArchiving(null)} />}
    </div>
  )
}
