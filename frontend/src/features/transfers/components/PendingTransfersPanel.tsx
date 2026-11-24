import { ArrowLeftRight, Loader2 } from 'lucide-react'
import { usePendingTransfers, useLinkTransfers } from '../api/useTransfers'
import type { TransferSide } from '../types/transfer.types'

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('pt-BR')
}

function directionLabel(side: TransferSide) {
  return side.direction === 'In' ? 'Entrada' : 'Saída'
}

// Pontas de transferência que ainda não têm a outra metade. Cada uma mostra a melhor sugestão
// de par (de outra conta) e permite vincular com um clique.
export default function PendingTransfersPanel() {
  const { data: pending = [], isLoading } = usePendingTransfers()
  const link = useLinkTransfers()

  if (isLoading || pending.length === 0) return null

  return (
    <section
      aria-label="Transferências sem par"
      className="rounded-xl overflow-hidden"
      style={{ border: '1px solid rgba(99,102,241,0.25)' }}
    >
      <div
        className="flex items-center gap-2 px-4 py-3 text-sm"
        style={{ background: 'rgba(99,102,241,0.08)', color: '#818cf8' }}
      >
        <ArrowLeftRight size={15} />
        <span className="font-medium">
          {pending.length} transferência{pending.length > 1 ? 's' : ''} sem par
        </span>
        <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
          — falta o lançamento na outra conta
        </span>
      </div>

      <ul className="divide-y" style={{ borderColor: 'var(--ff-border)' }}>
        {pending.map(({ transaction, suggestion }) => (
          <li
            key={transaction.transactionId}
            className="flex flex-wrap items-center justify-between gap-3 px-4 py-3"
            style={{ background: 'var(--ff-bg-card)' }}
          >
            <div className="min-w-0">
              <p className="text-sm truncate" style={{ color: 'var(--ff-text-primary)' }}>
                {transaction.description || 'Transferência'}
              </p>
              <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                {directionLabel(transaction)} · {transaction.accountName ?? 'Conta'} ·{' '}
                {formatDate(transaction.date)} · {brl.format(transaction.amount)}
              </p>
            </div>

            {suggestion ? (
              <div className="flex items-center gap-3">
                <p className="text-xs text-right" style={{ color: 'var(--ff-text-muted)' }}>
                  Par provável: {suggestion.candidate.accountName ?? 'outra conta'} ·{' '}
                  {formatDate(suggestion.candidate.date)}
                  {suggestion.willConvert && ' (vira transferência)'}
                </p>
                <button
                  onClick={() =>
                    link.mutate({
                      firstTransactionId:  transaction.transactionId,
                      secondTransactionId: suggestion.candidate.transactionId,
                    })
                  }
                  disabled={link.isPending}
                  aria-label={`Vincular ${transaction.description || 'transferência'}`}
                  className="flex items-center gap-1.5 h-8 px-3 rounded-lg text-xs font-medium disabled:opacity-50"
                  style={{ background: 'rgba(99,102,241,0.12)', color: '#818cf8', border: '1px solid rgba(99,102,241,0.3)' }}
                >
                  {link.isPending && <Loader2 size={12} className="animate-spin" />}
                  Vincular
                </button>
              </div>
            ) : (
              <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                Sem sugestão — importe o extrato da outra conta
              </p>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}
