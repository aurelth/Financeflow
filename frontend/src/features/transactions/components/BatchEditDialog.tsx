import { useState } from 'react'
import { useCategories } from '@/features/categories/api/useCategories'
import { useAccounts } from '@/features/accounts/api/useAccounts'
import { TransactionType } from '@/features/categories/types/category.types'
import { resolveType } from '@/lib/enumUtils'
import { useBatchUpdateTransactions } from '../api/useTransactions'
import { TransactionStatus, type Transaction } from '../types/transaction.types'

interface BatchEditDialogProps {
  transactions: Transaction[]
  onClose:      () => void
  // Chamado depois que o lote foi aplicado (a página limpa a seleção)
  onDone:       () => void
}

const fieldStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  borderRadius: '12px',
  padding:      '10px 16px',
  color:        'var(--ff-text-primary)',
  fontSize:     '14px',
  outline:      'none',
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

const STATUS_OPTIONS: { value: TransactionStatus; label: string }[] = [
  { value: TransactionStatus.Paid,      label: 'Pago' },
  { value: TransactionStatus.Pending,   label: 'Pendente' },
  { value: TransactionStatus.Scheduled, label: 'Agendado' },
]

// Edita vários lançamentos de uma vez: só os campos escolhidos são alterados
export default function BatchEditDialog({ transactions, onClose, onDone }: BatchEditDialogProps) {
  const { data: categories = [] } = useCategories()
  const { data: accounts = [] }   = useAccounts()
  const batchUpdate = useBatchUpdateTransactions()

  const [categoryId, setCategoryId] = useState('')
  const [accountId, setAccountId]   = useState('')
  const [status, setStatus]         = useState('')

  const types         = new Set(transactions.map(tx => resolveType(tx.type)))
  const hasIncome     = types.has(TransactionType.Income)
  const hasExpense    = types.has(TransactionType.Expense)
  const hasTransfer   = types.has(TransactionType.Transfer)
  const onlyTransfers = hasTransfer && !hasIncome && !hasExpense
  const mixedTypes    = hasIncome && hasExpense

  const activeCategories = categories.filter(c => c.isActive)
  const incomeCategories  = activeCategories.filter(c => c.type === TransactionType.Income)
  const expenseCategories = activeCategories.filter(c => c.type === TransactionType.Expense)

  const openAccounts = accounts.filter(a => !a.isArchived)
  const canPickAccount = openAccounts.length > 1

  const hasChange = !!categoryId || !!accountId || !!status

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!hasChange) return

    batchUpdate.mutate(
      {
        ids:        transactions.map(tx => tx.id),
        categoryId: categoryId || undefined,
        accountId:  accountId  || undefined,
        status:     status ? (Number(status) as TransactionStatus) : undefined,
      },
      { onSuccess: () => { onDone(); onClose() } },
    )
  }

  return (
    <div className="fixed inset-0 z-50 flex justify-center p-4 !m-0 overflow-y-auto [align-items:safe_center] bg-black/70 backdrop-blur-sm">
      <form
        onSubmit={handleSubmit}
        role="dialog"
        aria-modal="true"
        aria-labelledby="batch-edit-title"
        className="w-full max-w-md rounded-2xl p-6 shadow-2xl"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <h2 id="batch-edit-title" className="mb-1 text-lg font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
          Editar {transactions.length} {transactions.length === 1 ? 'transação' : 'transações'}
        </h2>
        <p className="mb-5 text-sm" style={{ color: 'var(--ff-text-muted)' }}>
          Preencha só o que deseja alterar. Os campos em branco continuam como estão.
        </p>

        <div className="space-y-4">
          <div>
            <label htmlFor="batch-category" style={labelStyle}>Categoria</label>
            <select
              id="batch-category"
              value={categoryId}
              onChange={e => setCategoryId(e.target.value)}
              disabled={onlyTransfers}
              style={fieldStyle}
            >
              <option value="">Não alterar</option>
              {mixedTypes ? (
                <>
                  <optgroup label="Receitas">
                    {incomeCategories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                  </optgroup>
                  <optgroup label="Despesas">
                    {expenseCategories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                  </optgroup>
                </>
              ) : (
                (hasIncome ? incomeCategories : expenseCategories)
                  .map(c => <option key={c.id} value={c.id}>{c.name}</option>)
              )}
            </select>
            {mixedTypes && (
              <p className="mt-1 text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                A seleção mistura receitas e despesas: os lançamentos de outro tipo ficam de fora.
              </p>
            )}
          </div>

          {canPickAccount && (
            <div>
              <label htmlFor="batch-account" style={labelStyle}>Conta</label>
              <select
                id="batch-account"
                value={accountId}
                onChange={e => setAccountId(e.target.value)}
                style={fieldStyle}
              >
                <option value="">Não alterar</option>
                {openAccounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
              </select>
            </div>
          )}

          <div>
            <label htmlFor="batch-status" style={labelStyle}>Status</label>
            <select
              id="batch-status"
              value={status}
              onChange={e => setStatus(e.target.value)}
              disabled={onlyTransfers}
              style={fieldStyle}
            >
              <option value="">Não alterar</option>
              {STATUS_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
          </div>

          {hasTransfer && (
            <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
              {onlyTransfers
                ? 'Transferências não têm categoria nem status alterados em lote. Só a conta pode mudar, e apenas nas que não têm par.'
                : 'Transferências ficam de fora de categoria e status, e só trocam de conta se não tiverem par.'}
            </p>
          )}
        </div>

        <div className="mt-6 flex gap-3">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 rounded-xl px-4 py-2.5 text-sm font-medium"
            style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={!hasChange || batchUpdate.isPending}
            className="flex-1 rounded-xl px-4 py-2.5 text-sm font-semibold disabled:cursor-not-allowed disabled:opacity-50"
            style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          >
            {batchUpdate.isPending ? 'Aplicando...' : 'Aplicar'}
          </button>
        </div>
      </form>
    </div>
  )
}
