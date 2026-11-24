import { useState } from 'react'
import { Unlink, X } from 'lucide-react'
import { useCategories } from '@/features/categories/api/useCategories'
import { TransactionType } from '@/features/categories/types/category.types'
import { useUnlinkTransfer } from '../api/useTransfers'

interface UnlinkTransferDialogProps {
  linkId:         string
  /** Descrição curta do par, ex.: "Conta A → Conta B" */
  summary?:       string
  onClose:        () => void
}

const selectStyle: React.CSSProperties = {
  width: '100%',
  background: 'var(--ff-bg-elevated)',
  border: '1px solid var(--ff-border)',
  borderRadius: '12px',
  padding: '10px 16px',
  color: 'var(--ff-text-primary)',
  fontSize: '14px',
  outline: 'none',
}

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontSize: '11px',
  fontWeight: 500,
  textTransform: 'uppercase',
  letterSpacing: '0.06em',
  color: 'var(--ff-text-muted)',
  marginBottom: '6px',
}

// Ao desfazer, a categoria original (perdida ao virar transferência) é escolhida de novo
export default function UnlinkTransferDialog({ linkId, summary, onClose }: UnlinkTransferDialogProps) {
  const { data: categories = [] } = useCategories()
  const unlink = useUnlinkTransfer(linkId)

  const expenseCategories = categories.filter(c => c.type === TransactionType.Expense)
  const incomeCategories  = categories.filter(c => c.type === TransactionType.Income)

  const [outCategoryId, setOutCategoryId] = useState('')
  const [inCategoryId, setInCategoryId]   = useState('')

  const canRevert = !!outCategoryId && !!inCategoryId && !unlink.isPending

  function handleRevert() {
    if (!canRevert) return
    unlink.mutate({ outCategoryId, inCategoryId, deleteTransactions: false }, { onSuccess: onClose })
  }

  function handleDelete() {
    unlink.mutate({ deleteTransactions: true }, { onSuccess: onClose })
  }

  return (
    <div className="fixed inset-0 z-50 flex justify-center p-4 !m-0 overflow-y-auto [align-items:safe_center] bg-black/70 backdrop-blur-sm">
      <div
        role="dialog"
        aria-label="Desfazer transferência"
        className="w-full max-w-md rounded-2xl shadow-2xl"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div
          className="flex items-center justify-between px-6 py-4"
          style={{ borderBottom: '1px solid var(--ff-border)' }}
        >
          <h2 className="font-semibold flex items-center gap-2" style={{ color: 'var(--ff-text-primary)' }}>
            <Unlink size={16} style={{ color: '#818cf8' }} />
            Desfazer transferência
          </h2>
          <button onClick={onClose} aria-label="Fechar" style={{ color: 'var(--ff-text-muted)' }}>
            <X size={18} />
          </button>
        </div>

        <div className="px-6 py-5 space-y-4">
          {summary && (
            <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>{summary}</p>
          )}
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Sem o vínculo, cada lado volta a ser um lançamento comum e passa a contar como despesa e receita.
            Escolha a categoria de cada um:
          </p>

          <div>
            <label htmlFor="unlink-out-category" style={labelStyle}>Categoria da saída (despesa)</label>
            <select
              id="unlink-out-category"
              value={outCategoryId}
              onChange={e => setOutCategoryId(e.target.value)}
              style={selectStyle}
            >
              <option value="">Selecione</option>
              {expenseCategories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>

          <div>
            <label htmlFor="unlink-in-category" style={labelStyle}>Categoria da entrada (receita)</label>
            <select
              id="unlink-in-category"
              value={inCategoryId}
              onChange={e => setInCategoryId(e.target.value)}
              style={selectStyle}
            >
              <option value="">Selecione</option>
              {incomeCategories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
        </div>

        <div
          className="flex items-center justify-between gap-2 px-6 py-4"
          style={{ borderTop: '1px solid var(--ff-border)' }}
        >
          <button
            onClick={handleDelete}
            disabled={unlink.isPending}
            className="h-9 px-3 rounded-xl text-xs font-medium disabled:opacity-50"
            style={{ color: '#f87171', border: '1px solid rgba(248,113,113,0.3)' }}
          >
            Excluir os dois lançamentos
          </button>
          <div className="flex items-center gap-2">
            <button onClick={onClose} className="h-9 px-4 rounded-xl text-sm" style={{ color: 'var(--ff-text-muted)' }}>
              Cancelar
            </button>
            <button
              onClick={handleRevert}
              disabled={!canRevert}
              className="h-9 px-4 rounded-xl text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed"
              style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
            >
              {unlink.isPending ? 'Salvando...' : 'Desfazer vínculo'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
