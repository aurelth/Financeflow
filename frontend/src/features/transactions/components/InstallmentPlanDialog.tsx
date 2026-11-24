import { useState } from 'react'
import { CreditCard, Loader2, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { resolveStatus } from '@/lib/enumUtils'
import { useCancelInstallments, useInstallmentPlan } from '../api/useTransactions'
import { formatBRL } from '../lib/installments'
import { TransactionStatus } from '../types/transaction.types'

interface InstallmentPlanDialogProps {
  groupId: string
  onClose: () => void
}

const STATUS_KEY: Record<TransactionStatus, string> = {
  [TransactionStatus.Paid]:      'status.paid',
  [TransactionStatus.Pending]:   'status.pending',
  [TransactionStatus.Scheduled]: 'status.scheduled',
}

export default function InstallmentPlanDialog({ groupId, onClose }: InstallmentPlanDialogProps) {
  const { t } = useTranslation('transactions')
  const { data: plan, isLoading, isError } = useInstallmentPlan(groupId)
  const cancel = useCancelInstallments()
  const [confirming, setConfirming] = useState(false)

  const hasOpenInstallments = !!plan?.installments.some(i => resolveStatus(i.status) !== TransactionStatus.Paid)

  function handleCancel() {
    cancel.mutate(groupId, {
      onSuccess: result => {
        toast.success(t('installments.cancelled', { cancelled: result.cancelledCount, kept: result.keptPaidCount }))
        onClose()
      },
    })
  }

  return (
    <div className="fixed inset-0 z-50 flex justify-center p-4 !m-0 overflow-y-auto [align-items:safe_center] bg-black/70 backdrop-blur-sm">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="installment-plan-title"
        className="w-full max-w-lg rounded-2xl shadow-2xl max-h-[calc(100dvh-2rem)] overflow-y-auto"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex items-center justify-between px-6 py-4" style={{ borderBottom: '1px solid var(--ff-border)' }}>
          <h2 id="installment-plan-title" className="flex items-center gap-2 font-semibold text-lg" style={{ color: 'var(--ff-text-primary)' }}>
            <CreditCard size={18} style={{ color: '#818cf8' }} />
            {t('installments.planTitle')}
          </h2>
          <button
            onClick={onClose}
            aria-label={t('installments.close')}
            className="p-1.5 rounded-lg"
            style={{ color: 'var(--ff-text-muted)' }}
          >
            <X size={18} />
          </button>
        </div>

        <div className="px-6 py-5 space-y-4">
          {isLoading && (
            <div className="flex justify-center py-6">
              <Loader2 size={20} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
            </div>
          )}

          {isError && (
            <p role="alert" className="text-sm" style={{ color: 'var(--ff-expense)' }}>{t('installments.loadError')}</p>
          )}

          {plan && (
            <>
              <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>{plan.description || '—'}</p>

              <div className="grid grid-cols-3 gap-1.5 sm:gap-2 text-center" data-testid="installment-summary">
                {([
                  ['installments.total',     plan.totalAmount],
                  ['installments.paid',      plan.paidAmount],
                  ['installments.remaining', plan.remainingAmount],
                ] as const).map(([key, value]) => (
                  <div key={key} className="rounded-xl px-2 py-2" style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}>
                    <p className="text-[11px] uppercase tracking-wide" style={{ color: 'var(--ff-text-muted)' }}>{t(key)}</p>
                    <p className="text-xs sm:text-sm font-semibold break-words" style={{ color: 'var(--ff-text-primary)' }}>{formatBRL(value)}</p>
                  </div>
                ))}
              </div>

              <ul className="space-y-1.5" aria-label={t('installments.planTitle')}>
                {plan.installments.map(item => {
                  const status = resolveStatus(item.status)
                  return (
                    <li
                      key={item.id}
                      data-testid="installment-row"
                      className="flex items-center justify-between rounded-xl px-3 py-2 text-sm"
                      style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
                    >
                      <span style={{ color: 'var(--ff-text-secondary)' }}>
                        {item.number}/{plan.count} · {new Date(item.date).toLocaleDateString('pt-BR')}
                      </span>
                      <span className="flex items-center gap-3">
                        <span className="text-xs" style={{ color: status === TransactionStatus.Paid ? 'var(--ff-income)' : 'var(--ff-text-muted)' }}>
                          {t(STATUS_KEY[status])}
                        </span>
                        <span className="font-medium" style={{ color: 'var(--ff-text-primary)' }}>{formatBRL(item.amount)}</span>
                      </span>
                    </li>
                  )
                })}
              </ul>

              {confirming && (
                <div
                  role="alertdialog"
                  className="rounded-xl px-3 py-3 text-sm space-y-3"
                  style={{ background: 'rgba(244,63,94,0.06)', border: '1px solid rgba(244,63,94,0.3)', color: 'var(--ff-text-primary)' }}
                >
                  <p>{t('installments.cancelConfirm')}</p>
                  <div className="flex gap-2">
                    <button
                      onClick={() => setConfirming(false)}
                      disabled={cancel.isPending}
                      className="flex-1 h-9 rounded-xl text-sm"
                      style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
                    >
                      {t('installments.keep')}
                    </button>
                    <button
                      onClick={handleCancel}
                      disabled={cancel.isPending}
                      className="flex-1 h-9 rounded-xl text-sm font-semibold disabled:opacity-50"
                      style={{ background: 'var(--ff-expense)', color: '#fff' }}
                    >
                      {cancel.isPending ? t('installments.cancelling') : t('installments.confirmCancel')}
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>

        <div className="flex gap-3 px-6 py-4" style={{ borderTop: '1px solid var(--ff-border)' }}>
          <button
            onClick={onClose}
            className="flex-1 h-10 rounded-xl text-sm font-medium"
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-muted)' }}
          >
            {t('installments.close')}
          </button>
          {plan && !confirming && (
            hasOpenInstallments ? (
              <button
                onClick={() => setConfirming(true)}
                className="flex-1 h-10 rounded-xl text-sm font-semibold"
                style={{ background: 'rgba(244,63,94,0.1)', border: '1px solid rgba(244,63,94,0.3)', color: 'var(--ff-expense)' }}
              >
                {t('installments.cancelPurchase')}
              </button>
            ) : (
              <p className="flex-1 flex items-center text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                {t('installments.nothingToCancel')}
              </p>
            )
          )}
        </div>
      </div>
    </div>
  )
}
