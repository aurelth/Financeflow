import { useState } from 'react'
import { X, ArrowLeftRight } from 'lucide-react'
import { useAccounts } from '@/features/accounts/api/useAccounts'
import { useCreateTransfer } from '../api/useTransfers'

interface TransferFormDialogProps {
  onClose: () => void
}

const inputStyle: React.CSSProperties = {
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

// Cria as duas pontas (saída na origem, entrada no destino) e o vínculo numa só chamada
export default function TransferFormDialog({ onClose }: TransferFormDialogProps) {
  const { data: accounts = [] } = useAccounts()
  const createTransfer = useCreateTransfer()

  const [fromAccountId, setFromAccountId] = useState('')
  const [toAccountId, setToAccountId]     = useState('')
  const [amount, setAmount]               = useState('')
  const [date, setDate]                   = useState(new Date().toISOString().split('T')[0])
  const [description, setDescription]     = useState('')

  const value = Number(amount)
  const sameAccount = !!fromAccountId && fromAccountId === toAccountId
  const canSubmit =
    !!fromAccountId && !!toAccountId && !sameAccount && value > 0 && !!date && !createTransfer.isPending

  function handleSubmit() {
    if (!canSubmit) return

    createTransfer.mutate(
      {
        fromAccountId,
        toAccountId,
        amount: value,
        date,
        description: description.trim() || undefined,
      },
      { onSuccess: onClose },
    )
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div
        role="dialog"
        aria-label="Transferir entre contas"
        className="w-full max-w-md rounded-2xl shadow-2xl"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div
          className="flex items-center justify-between px-6 py-4"
          style={{ borderBottom: '1px solid var(--ff-border)' }}
        >
          <h2 className="font-semibold flex items-center gap-2" style={{ color: 'var(--ff-text-primary)' }}>
            <ArrowLeftRight size={16} style={{ color: '#818cf8' }} />
            Transferir entre contas
          </h2>
          <button onClick={onClose} aria-label="Fechar" style={{ color: 'var(--ff-text-muted)' }}>
            <X size={18} />
          </button>
        </div>

        <div className="px-6 py-5 space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="transfer-from" style={labelStyle}>De</label>
              <select
                id="transfer-from"
                value={fromAccountId}
                onChange={e => setFromAccountId(e.target.value)}
                style={inputStyle}
              >
                <option value="">Selecione</option>
                {accounts.map(a => (
                  <option key={a.id} value={a.id}>{a.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="transfer-to" style={labelStyle}>Para</label>
              <select
                id="transfer-to"
                value={toAccountId}
                onChange={e => setToAccountId(e.target.value)}
                style={inputStyle}
              >
                <option value="">Selecione</option>
                {accounts.map(a => (
                  <option key={a.id} value={a.id}>{a.name}</option>
                ))}
              </select>
            </div>
          </div>

          {sameAccount && (
            <p role="alert" className="text-xs" style={{ color: '#f87171' }}>
              A conta de origem e a de destino devem ser diferentes.
            </p>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="transfer-amount" style={labelStyle}>Valor</label>
              <input
                id="transfer-amount"
                type="number" min="0" step="0.01"
                value={amount}
                onChange={e => setAmount(e.target.value)}
                placeholder="0,00"
                style={inputStyle}
              />
            </div>
            <div>
              <label htmlFor="transfer-date" style={labelStyle}>Data</label>
              <input
                id="transfer-date"
                type="date"
                value={date}
                onChange={e => setDate(e.target.value)}
                style={inputStyle}
              />
            </div>
          </div>

          <div>
            <label htmlFor="transfer-description" style={labelStyle}>Descrição (opcional)</label>
            <input
              id="transfer-description"
              type="text"
              value={description}
              onChange={e => setDescription(e.target.value)}
              placeholder="Ex: Reserva do mês"
              style={inputStyle}
            />
          </div>

          <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            O valor sai de uma conta e entra na outra. Não conta como receita nem despesa.
          </p>
        </div>

        <div
          className="flex items-center justify-end gap-2 px-6 py-4"
          style={{ borderTop: '1px solid var(--ff-border)' }}
        >
          <button
            onClick={onClose}
            className="h-9 px-4 rounded-xl text-sm"
            style={{ color: 'var(--ff-text-muted)' }}
          >
            Cancelar
          </button>
          <button
            onClick={handleSubmit}
            disabled={!canSubmit}
            className="h-9 px-4 rounded-xl text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          >
            {createTransfer.isPending ? 'Salvando...' : 'Transferir'}
          </button>
        </div>
      </div>
    </div>
  )
}
