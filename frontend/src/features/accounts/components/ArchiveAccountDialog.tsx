import { Archive } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useArchiveAccount } from '../api/useAccounts'
import type { Account } from '../types/account.types'

interface ArchiveAccountDialogProps {
  account: Account
  onClose: () => void
}

export default function ArchiveAccountDialog({ account, onClose }: ArchiveAccountDialogProps) {
  const { t } = useTranslation('accounts')
  const archive = useArchiveAccount()

  function handleConfirm() {
    archive.mutate(account.id, {
      onSuccess: () => { toast.success(t('toast.archived')); onClose() },
    })
  }

  return (
    <div className="fixed inset-0 z-50 flex justify-center p-4 !m-0 overflow-y-auto [align-items:safe_center] bg-black/70 backdrop-blur-sm">
      <div
        role="alertdialog"
        aria-label={t('archiveDialog.title')}
        className="rounded-2xl p-6 w-full max-w-md shadow-2xl"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div
          className="w-12 h-12 rounded-xl flex items-center justify-center mb-4"
          style={{ background: 'rgba(248,113,113,0.1)' }}
        >
          <Archive size={22} style={{ color: '#f87171' }} />
        </div>
        <h2 className="font-semibold text-lg mb-1" style={{ color: 'var(--ff-text-primary)' }}>
          {t('archiveDialog.title')}
        </h2>
        <p className="text-sm mb-6" style={{ color: 'var(--ff-text-muted)' }}>
          {t('archiveDialog.message', { name: account.name })}
        </p>
        <div className="flex justify-end gap-2">
          <button
            onClick={onClose}
            className="h-9 px-4 rounded-xl text-sm"
            style={{ color: 'var(--ff-text-muted)' }}
          >
            {t('archiveDialog.cancel')}
          </button>
          <button
            onClick={handleConfirm}
            disabled={archive.isPending}
            className="h-9 px-5 rounded-xl text-sm font-medium disabled:opacity-50"
            style={{ background: '#ef4444', color: '#fff' }}
          >
            {t('archiveDialog.confirm')}
          </button>
        </div>
      </div>
    </div>
  )
}
