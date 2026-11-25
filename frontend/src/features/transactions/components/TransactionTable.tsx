import { Pencil, Trash2, RefreshCw, ArrowLeftRight, Unlink, CreditCard } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { TransactionStatus, RecurrenceType, type Transaction } from '../types/transaction.types'
import { TransactionType } from '../../categories/types/category.types'
import CategoryIcon  from '../../categories/components/CategoryIcon'
import AttachmentViewer from './AttachmentViewer'
import { resolveType, resolveStatus } from '@/lib/enumUtils'

interface TransactionTableProps {
  transactions: Transaction[]
  onEdit:       (transaction: Transaction) => void
  onDelete:     (transaction: Transaction) => void
  // Desfaz o vínculo de uma transferência pareada (aparece só nas transferências com par)
  onUnlink?:    (transaction: Transaction) => void
  // Abre o plano de parcelas de uma compra parcelada (o selo "3/12" vira botão quando informado)
  onShowInstallments?: (transaction: Transaction) => void
  // Mostra o nome da conta sob a descrição (útil quando o utilizador tem mais de uma conta)
  showAccount?: boolean
  // Seleção múltipla para as ações em lote (sem isso a tabela não mostra caixas de seleção)
  selection?:   TransactionSelection
}

export interface TransactionSelection {
  selectedIds: ReadonlySet<string>
  onToggle:    (id: string) => void
  // Marca todas as linhas da página; se todas já estão marcadas, desmarca
  onToggleAll: () => void
}

const statusConfig: Record<TransactionStatus, { label: string; bg: string; color: string; border: string }> = {
  [TransactionStatus.Paid]: {
    label:  'Pago',
    bg:     'rgba(16,185,129,0.1)',
    color:  'var(--ff-income)',
    border: 'rgba(16,185,129,0.2)',
  },
  [TransactionStatus.Pending]: {
    label:  'Pendente',
    bg:     'rgba(245,158,11,0.1)',
    color:  'var(--ff-pending)',
    border: 'rgba(245,158,11,0.2)',
  },
  [TransactionStatus.Scheduled]: {
    label:  'Agendado',
    bg:     'rgba(99,102,241,0.1)',
    color:  'var(--ff-scheduled)',
    border: 'rgba(99,102,241,0.2)',
  },
}

const recurrenceLabel: Record<RecurrenceType, string> = {
  [RecurrenceType.None]:    '',
  [RecurrenceType.Daily]:   'Diária',
  [RecurrenceType.Weekly]:  'Semanal',
  [RecurrenceType.Monthly]: 'Mensal',
  [RecurrenceType.Yearly]:  'Anual',
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('pt-BR')
}

function formatAmount(amount: number, type: TransactionType) {
  const formatted = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(amount)
  if (type === TransactionType.Income)   return `+${formatted}`
  if (type === TransactionType.Transfer) return `↔ ${formatted}`
  return `-${formatted}`
}

// Cor do valor por tipo
function getAmountColor(type: TransactionType): string {
  if (type === TransactionType.Income)   return 'var(--ff-income)'
  if (type === TransactionType.Transfer) return '#818cf8'
  return 'var(--ff-expense)'
}

export default function TransactionTable({ transactions, onEdit, onDelete, onUnlink, onShowInstallments, showAccount = false, selection }: TransactionTableProps) {
  const { t } = useTranslation('transactions')

  const selectedCount = selection ? transactions.filter(tx => selection.selectedIds.has(tx.id)).length : 0
  const allSelected   = selection !== undefined && selectedCount === transactions.length && transactions.length > 0
  const someSelected  = selectedCount > 0 && !allSelected

  // Com seleção, o card mobile ganha uma coluna à esquerda para a caixa e as demais células andam uma coluna
  const col1 = selection ? 'max-md:col-start-2' : 'max-md:col-start-1'
  const col2 = selection ? 'max-md:col-start-3' : 'max-md:col-start-2'

  if (transactions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
          Nenhuma transação encontrada.
        </p>
      </div>
    )
  }

  return (
    <div
      className="rounded-xl overflow-hidden"
      style={{ border: '1px solid var(--ff-border)' }}
    >
      {/* No mobile o cabeçalho some, então "selecionar todos" ganha uma barra própria */}
      {selection && (
        <label
          className="flex items-center gap-3 px-4 py-3 text-sm md:hidden"
          style={{ background: 'var(--ff-bg-card)', borderBottom: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
        >
          <input
            type="checkbox"
            className="size-5 cursor-pointer accent-[var(--ff-emerald)]"
            checked={allSelected}
            ref={el => { if (el) el.indeterminate = someSelected }}
            onChange={selection.onToggleAll}
          />
          Selecionar todos da página
        </label>
      )}
      <table className="w-full text-sm max-md:block">
        <thead className="max-md:hidden">
          <tr style={{ background: 'var(--ff-bg-card)', borderBottom: '1px solid var(--ff-border)' }}>
            {selection && (
              <th className="w-10 px-3 py-3">
                <input
                  type="checkbox"
                  className="size-4 cursor-pointer accent-[var(--ff-emerald)]"
                  aria-label="Selecionar todas as transações da página"
                  checked={allSelected}
                  ref={el => { if (el) el.indeterminate = someSelected }}
                  onChange={selection.onToggleAll}
                />
              </th>
            )}
            {['Descrição', 'Categoria', 'Data', 'Status', 'Valor', 'Ações'].map((h, i) => (
              <th
                key={h}
                className={`px-4 py-3 font-medium text-xs uppercase tracking-wide ${i >= 4 ? 'text-right' : 'text-left'}`}
                style={{ color: 'var(--ff-text-muted)' }}
              >
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="max-md:block">
          {transactions.map(tx => {
            const type   = resolveType(tx.type)
            const status = resolveStatus(tx.status)
            const s      = statusConfig[status] ?? statusConfig[TransactionStatus.Paid]
            const isSelected = selection?.selectedIds.has(tx.id) ?? false
            const rowBackground = isSelected ? 'rgba(16,185,129,0.07)' : 'var(--ff-bg-card)'

            return (
              <tr
                key={tx.id}
                aria-selected={selection ? isSelected : undefined}
                className={`transition-colors duration-150 max-md:grid max-md:gap-x-3 max-md:gap-y-2 max-md:p-3 ${
                  selection ? 'max-md:grid-cols-[auto_minmax(0,1fr)_auto]' : 'max-md:grid-cols-[minmax(0,1fr)_auto]'
                }`}
                style={{ background: rowBackground, borderBottom: '1px solid var(--ff-border-subtle)' }}
                onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
                onMouseLeave={e => (e.currentTarget.style.background = rowBackground)}
              >
                {selection && (
                  <td className="w-10 px-3 py-3 max-md:col-start-1 max-md:row-span-3 max-md:row-start-1 max-md:self-start max-md:p-0 max-md:pt-0.5">
                    <input
                      type="checkbox"
                      className="size-4 cursor-pointer accent-[var(--ff-emerald)] max-md:size-5"
                      aria-label={`Selecionar ${tx.description || 'transação'}`}
                      checked={isSelected}
                      onChange={() => selection.onToggle(tx.id)}
                    />
                  </td>
                )}

                {/* Descrição */}
                <td className={`px-4 py-3 max-md:p-0 ${col1} max-md:row-start-1 max-md:min-w-0`}>
                  <div className="flex items-center gap-2 flex-wrap">
                    <span
                      className="font-medium md:truncate md:max-w-[200px] max-md:break-words max-md:min-w-0"
                      style={{ color: 'var(--ff-text-primary)' }}
                    >
                      {tx.description || '—'}
                    </span>
                    {tx.attachmentPath && (
                      <AttachmentViewer
                        transactionId={tx.id}
                        fileName={tx.attachmentName ?? tx.attachmentPath.split('/').pop() ?? 'comprovante'}
                        triggerIcon="paperclip"
                      />
                    )}
                    {tx.installmentGroupId && tx.installmentNumber && tx.installmentCount && (
                      <button
                        type="button"
                        data-testid="installment-badge"
                        onClick={() => onShowInstallments?.(tx)}
                        disabled={!onShowInstallments}
                        title={t('installments.badgeTitle', { number: tx.installmentNumber, count: tx.installmentCount })}
                        className="flex items-center gap-1 text-xs px-1.5 py-0.5 rounded-md flex-shrink-0 disabled:cursor-default"
                        style={{
                          background: 'rgba(99,102,241,0.1)',
                          color:      '#818cf8',
                          border:     '1px solid rgba(99,102,241,0.25)',
                        }}
                      >
                        <CreditCard size={10} />
                        {tx.installmentNumber}/{tx.installmentCount}
                      </button>
                    )}
                    {tx.isRecurring && recurrenceLabel[tx.recurrenceType] && (
                      <span
                        className="flex items-center gap-1 text-xs px-1.5 py-0.5 rounded-md flex-shrink-0"
                        style={{
                          background: 'var(--ff-emerald-subtle)',
                          color:      'var(--ff-emerald)',
                          border:     '1px solid rgba(16,185,129,0.2)',
                        }}
                      >
                        <RefreshCw size={10} />
                        {recurrenceLabel[tx.recurrenceType]}
                      </span>
                    )}
                    {/* Badge Transfer */}
                    {type === TransactionType.Transfer && (
                      <span
                        className="flex items-center gap-1 text-xs px-1.5 py-0.5 rounded-md flex-shrink-0"
                        style={{
                          background: 'rgba(99,102,241,0.1)',
                          color:      '#818cf8',
                          border:     '1px solid rgba(99,102,241,0.2)',
                        }}
                      >
                        <ArrowLeftRight size={10} />
                        Transferência
                      </span>
                    )}
                    {/* Outra ponta da transferência: para onde foi / de onde veio, ou "sem par" */}
                    {type === TransactionType.Transfer && tx.transferDirection && (
                      <span
                        className="text-xs flex-shrink-0"
                        style={{ color: tx.transferLinkId ? 'var(--ff-text-muted)' : 'var(--ff-pending)' }}
                      >
                        {tx.transferLinkId
                          ? `${tx.transferDirection === 'Out' ? 'para' : 'de'} ${tx.counterpartAccountName ?? 'outra conta'}`
                          : 'sem par'}
                      </span>
                    )}
                  </div>
                  {showAccount && tx.accountName && (
                    <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
                      {tx.accountName}
                    </p>
                  )}
                  {tx.tags.length > 0 && (
                    <div className="flex gap-1 mt-1 flex-wrap">
                      {tx.tags.map(tag => (
                        <span
                          key={tag}
                          className="text-xs px-1.5 py-0.5 rounded-md"
                          style={{
                            background: 'var(--ff-bg-elevated)',
                            color:      'var(--ff-text-muted)',
                          }}
                        >
                          {tag}
                        </span>
                      ))}
                    </div>
                  )}
                </td>

                {/* Categoria */}
                <td className={`px-4 py-3 max-md:p-0 ${col1} max-md:row-start-2 max-md:min-w-0`}>
                  <div className="flex items-center gap-2">
                    {/* Ícone especial para Transfer */}
                    {type === TransactionType.Transfer ? (
                      <span
                        className="w-6 h-6 rounded-md flex items-center justify-center flex-shrink-0"
                        style={{ background: 'rgba(99,102,241,0.1)' }}
                      >
                        <ArrowLeftRight size={12} style={{ color: '#818cf8' }} />
                      </span>
                    ) : (
                      <span
                        className="w-6 h-6 rounded-md flex items-center justify-center flex-shrink-0"
                        style={{
                          backgroundColor: tx.categoryColor ? `${tx.categoryColor}20` : 'var(--ff-bg-elevated)',
                        }}
                      >
                        <CategoryIcon
                          icon={tx.categoryIcon  ?? 'ellipsis'}
                          color={tx.categoryColor ?? 'var(--ff-text-muted)'}
                          size={14}
                        />
                      </span>
                    )}
                    <div className="min-w-0">
                      <p className="truncate" style={{ color: 'var(--ff-text-secondary)' }}>
                        {tx.categoryName ?? '—'}
                      </p>
                      {tx.subcategoryName && (
                        <p className="text-xs truncate" style={{ color: 'var(--ff-text-muted)' }}>
                          {tx.subcategoryName}
                        </p>
                      )}
                    </div>
                  </div>
                </td>

                {/* Data */}
                <td className={`px-4 py-3 whitespace-nowrap max-md:p-0 ${col2} max-md:row-start-2 max-md:text-right max-md:text-xs max-md:self-center`} style={{ color: 'var(--ff-text-muted)' }}>
                  {formatDate(tx.date)}
                </td>

                {/* Status */}
                <td className={`px-4 py-3 max-md:p-0 ${col1} max-md:row-start-3 max-md:self-center`}>
                  {type === TransactionType.Transfer ? (                    
                    <span
                      className="text-xs px-2 py-0.5 rounded-full font-medium"
                      style={{ background: 'rgba(99,102,241,0.1)', color: '#818cf8', border: '1px solid rgba(99,102,241,0.2)' }}
                    >
                      Transferência
                    </span>
                  ) : (
                    <span
                      className="text-xs px-2 py-0.5 rounded-full font-medium"
                      style={{ background: s.bg, color: s.color, border: `1px solid ${s.border}` }}
                    >
                      {s.label}
                    </span>
                  )}
                </td>

                {/* Valor */}
                <td className={`px-4 py-3 text-right whitespace-nowrap font-semibold max-md:p-0 ${col2} max-md:row-start-1`}>
                  <span style={{ color: getAmountColor(type) }}>
                    {formatAmount(tx.amount, type)}
                  </span>
                </td>

                {/* Ações */}
                <td className={`px-4 py-3 max-md:p-0 ${col2} max-md:row-start-3`}>
                  <div className="flex items-center justify-end gap-1">
                    {onUnlink && tx.transferLinkId && (
                      <button
                        onClick={() => onUnlink(tx)}
                        className="p-1.5 max-md:p-2.5 rounded-lg transition-all duration-200"
                        style={{ color: 'var(--ff-text-muted)' }}
                        onMouseEnter={e => {
                          e.currentTarget.style.color      = '#818cf8'
                          e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                        }}
                        onMouseLeave={e => {
                          e.currentTarget.style.color      = 'var(--ff-text-muted)'
                          e.currentTarget.style.background = 'transparent'
                        }}
                        title="Desfazer vínculo da transferência"
                        aria-label={`Desfazer vínculo: ${tx.description || 'transferência'}`}
                      >
                        <Unlink size={14} />
                      </button>
                    )}
                    <button
                      onClick={() => onEdit(tx)}
                      className="p-1.5 max-md:p-2.5 rounded-lg transition-all duration-200"
                      style={{ color: 'var(--ff-text-muted)' }}
                      onMouseEnter={e => {
                        e.currentTarget.style.color      = 'var(--ff-text-primary)'
                        e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                      }}
                      onMouseLeave={e => {
                        e.currentTarget.style.color      = 'var(--ff-text-muted)'
                        e.currentTarget.style.background = 'transparent'
                      }}
                      title="Editar"
                    >
                      <Pencil size={14} />
                    </button>
                    <button
                      onClick={() => onDelete(tx)}
                      className="p-1.5 max-md:p-2.5 rounded-lg transition-all duration-200"
                      style={{ color: 'var(--ff-text-muted)' }}
                      onMouseEnter={e => {
                        e.currentTarget.style.color      = 'var(--ff-expense)'
                        e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                      }}
                      onMouseLeave={e => {
                        e.currentTarget.style.color      = 'var(--ff-text-muted)'
                        e.currentTarget.style.background = 'transparent'
                      }}
                      title="Remover"
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}