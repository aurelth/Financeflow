import { useState } from 'react'
import { X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import ColorPicker from '@/features/categories/components/ColorPicker'
import { useCreateAccount, useUpdateAccount } from '../api/useAccounts'
import { ACCOUNT_TYPES, type Account, type AccountType } from '../types/account.types'

interface AccountFormProps {
  account?: Account
  onClose:  () => void
}

const inputStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  borderRadius: '12px',
  padding:      '10px 16px',
  color:        'var(--ff-text-primary)',
  fontSize:     '14px',
  outline:      'none',
  transition:   'border-color 0.15s',
}

const labelStyle: React.CSSProperties = {
  display:       'block',
  fontSize:      '11px',
  fontWeight:    500,
  textTransform: 'uppercase',
  letterSpacing: '0.06em',
  color:         'var(--ff-text-muted)',
  marginBottom:  '6px',
}

export default function AccountForm({ account, onClose }: AccountFormProps) {
  const { t } = useTranslation('accounts')
  const isEditing = !!account

  const [name, setName]               = useState(account?.name ?? '')
  const [type, setType]               = useState<AccountType>(account?.type ?? 'Checking')
  const [bankName, setBankName]       = useState(account?.bankName ?? '')
  const [initialBalance, setBalance]  = useState<number>(account?.initialBalance ?? 0)
  const [balanceDate, setBalanceDate] = useState(account?.initialBalanceDate?.split('T')[0] ?? '')
  const [color, setColor]             = useState(account?.color ?? '#10b981')

  const createAccount = useCreateAccount()
  const updateAccount = useUpdateAccount(account?.id ?? '')
  const isPending     = createAccount.isPending || updateAccount.isPending

  function handleSubmit() {
    if (!name.trim()) {
      toast.error(t('form.nameRequired'))
      return
    }

    const payload = {
      name:               name.trim(),
      type,
      bankName:           bankName.trim() || null,
      initialBalance,
      initialBalanceDate: balanceDate || null,
      currency:           account?.currency ?? null,
      color,
      icon:               account?.icon ?? null,
    }

    if (isEditing) {
      updateAccount.mutate(
        { ...payload, isArchived: account.isArchived },
        { onSuccess: () => { toast.success(t('toast.updated')); onClose() } },
      )
    } else {
      createAccount.mutate(payload, {
        onSuccess: () => { toast.success(t('toast.created')); onClose() },
      })
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex justify-center p-4 !m-0 overflow-y-auto [align-items:safe_center] bg-black/70 backdrop-blur-sm">
      <div
        role="dialog"
        aria-label={isEditing ? t('form.editTitle') : t('form.createTitle')}
        className="w-full max-w-md rounded-2xl shadow-2xl max-h-[calc(100dvh-2rem)] overflow-y-auto"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div
          className="flex items-center justify-between px-6 py-4"
          style={{ borderBottom: '1px solid var(--ff-border)' }}
        >
          <h2 className="font-semibold text-lg" style={{ color: 'var(--ff-text-primary)' }}>
            {isEditing ? t('form.editTitle') : t('form.createTitle')}
          </h2>
          <button
            onClick={onClose}
            aria-label={t('form.close')}
            className="p-1.5 rounded-lg transition-all"
            style={{ color: 'var(--ff-text-muted)' }}
          >
            <X size={18} />
          </button>
        </div>

        <div className="px-6 py-5 space-y-4">
          <div>
            <label htmlFor="account-name" style={labelStyle}>{t('form.name')}</label>
            <input
              id="account-name"
              type="text"
              value={name}
              maxLength={60}
              onChange={e => setName(e.target.value)}
              placeholder={t('form.namePlaceholder')}
              style={inputStyle}
            />
          </div>

          <div className="grid grid-cols-1 min-[420px]:grid-cols-2 gap-3">
            <div>
              <label htmlFor="account-type" style={labelStyle}>{t('form.type')}</label>
              <select
                id="account-type"
                value={type}
                onChange={e => setType(e.target.value as AccountType)}
                style={inputStyle}
              >
                {ACCOUNT_TYPES.map(at => (
                  <option key={at} value={at}>{t(`types.${at}`)}</option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="account-bank" style={labelStyle}>{t('form.bankName')}</label>
              <input
                id="account-bank"
                type="text"
                value={bankName}
                maxLength={80}
                onChange={e => setBankName(e.target.value)}
                placeholder={t('form.bankNamePlaceholder')}
                style={inputStyle}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 min-[420px]:grid-cols-2 gap-3">
            <div>
              <label htmlFor="account-balance" style={labelStyle}>{t('form.initialBalance')}</label>
              <input
                id="account-balance"
                type="number"
                step="0.01"
                value={initialBalance}
                onChange={e => setBalance(Number(e.target.value))}
                style={inputStyle}
              />
            </div>
            <div>
              <label htmlFor="account-balance-date" style={labelStyle}>{t('form.initialBalanceDate')}</label>
              <input
                id="account-balance-date"
                type="date"
                value={balanceDate}
                onChange={e => setBalanceDate(e.target.value)}
                style={inputStyle}
              />
            </div>
          </div>
          <p className="text-xs -mt-2" style={{ color: 'var(--ff-text-muted)' }}>
            {t('form.initialBalanceHint')}
          </p>

          <div>
            <label style={labelStyle}>{t('form.color')}</label>
            <ColorPicker value={color} onChange={setColor} />
          </div>
        </div>

        <div
          className="flex justify-end gap-2 px-6 py-4"
          style={{ borderTop: '1px solid var(--ff-border)' }}
        >
          <button
            onClick={onClose}
            className="h-9 px-4 rounded-xl text-sm transition-colors"
            style={{ color: 'var(--ff-text-muted)' }}
          >
            {t('form.cancel')}
          </button>
          <button
            onClick={handleSubmit}
            disabled={isPending}
            className="h-9 px-5 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
            style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          >
            {isPending ? t('form.saving') : t('form.save')}
          </button>
        </div>
      </div>
    </div>
  )
}
