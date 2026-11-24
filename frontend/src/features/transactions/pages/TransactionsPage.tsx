import { useState } from 'react'
import { ArrowLeftRight, Plus, Receipt, Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useTransactions }           from '../api/useTransactions'
import { useCategories }             from '../../categories/api/useCategories'
import { useAccounts }               from '@/features/accounts/api/useAccounts'
import TransactionTable              from '../components/TransactionTable'
import TransactionFilters            from '../components/TransactionFilters'
import TransactionForm               from '../components/TransactionForm'
import DeleteTransactionDialog       from '../components/DeleteTransactionDialog'
import InstallmentPlanDialog         from '../components/InstallmentPlanDialog'
import TransferFormDialog             from '@/features/transfers/components/TransferFormDialog'
import UnlinkTransferDialog          from '@/features/transfers/components/UnlinkTransferDialog'
import PendingTransfersPanel         from '@/features/transfers/components/PendingTransfersPanel'
import QuickEntryDialog              from '@/features/quick-entry/components/QuickEntryDialog'
import RecurringSuggestionsPanel     from '@/features/recurring/components/RecurringSuggestionsPanel'
import ExportButton                  from '@/features/reports/components/ExportButton'
import EmptyState                    from '@/components/ui/EmptyState'
import SkeletonTable                 from '@/components/ui/SkeletonTable'
import type { Transaction, GetTransactionsQuery } from '../types/transaction.types'
import type { QuickEntryBatch } from '@/features/quick-entry/types/quickEntry.types'

function getDefaultFilters(): GetTransactionsQuery {
  const now      = new Date()
  const year     = now.getFullYear()
  const month    = now.getMonth()
  const firstDay = new Date(year, month, 1)
  const lastDay  = new Date(year, month + 1, 0)
  const toDateString = (d: Date) => d.toISOString().split('T')[0]
  return { page: 1, pageSize: 20, dateFrom: toDateString(firstDay), dateTo: toDateString(lastDay) }
}

const DEFAULT_FILTERS: GetTransactionsQuery = getDefaultFilters()

export default function TransactionsPage() {
  const { t } = useTranslation('transactions')
  const [filters, setFilters]       = useState<GetTransactionsQuery>(DEFAULT_FILTERS)
  const [showForm, setShowForm]     = useState(false)
  const [editingTx, setEditingTx]   = useState<Transaction | null>(null)
  const [deletingTx, setDeletingTx] = useState<Transaction | null>(null)
  const [showTransferForm, setShowTransferForm] = useState(false)
  const [unlinkingTx, setUnlinkingTx] = useState<Transaction | null>(null)
  const [showQuickEntry, setShowQuickEntry] = useState(false)
  const [planGroupId, setPlanGroupId] = useState<string | null>(null)
  // Fila de rascunhos da IA: o formulário abre um por vez, em ordem
  const [aiBatch, setAiBatch]       = useState<QuickEntryBatch | null>(null)
  const [aiIndex, setAiIndex]       = useState(0)

  const { data, isLoading }       = useTransactions(filters)
  const { data: categories = [] } = useCategories()
  const { data: accounts = [] }   = useAccounts()

  const transactions = data?.items      ?? []
  const totalPages   = data?.totalPages ?? 1
  const currentPage  = filters.page     ?? 1

  const filterMonth = filters.dateFrom ? new Date(filters.dateFrom).getMonth() + 1 : undefined
  const filterYear  = filters.dateFrom ? new Date(filters.dateFrom).getFullYear()  : undefined

  const isEmpty = !isLoading && transactions.length === 0

  function handleEdit(tx: Transaction) {
    setEditingTx(tx)
    setShowForm(true)
  }

  function handleCloseForm() {
    setShowForm(false)
    setEditingTx(null)
  }

  function handleQuickEntryResult(batch: QuickEntryBatch) {
    setShowQuickEntry(false)
    setAiBatch(batch)
    setAiIndex(0)
  }

  // Salvar ou fechar o formulário passa para o próximo rascunho; no último, encerra a fila
  function handleCloseDraft() {
    if (aiBatch && aiIndex + 1 < aiBatch.drafts.length) setAiIndex(i => i + 1)
    else setAiBatch(null)
  }

  function handleFilterChange(newFilters: GetTransactionsQuery) {
    setFilters({ ...newFilters, page: 1 })
  }

  function handlePageChange(page: number) {
    setFilters(f => ({ ...f, page }))
  }

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            Transações
          </h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Registre e acompanhe as suas receitas e despesas
          </p>
        </div>
        <div className="flex items-center gap-2">
          <ExportButton defaultMonth={filterMonth} defaultYear={filterYear} />
          {accounts.length > 1 && (
            <button
              onClick={() => setShowTransferForm(true)}
              className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors"
              style={{ background: 'rgba(99,102,241,0.1)', color: '#818cf8', border: '1px solid rgba(99,102,241,0.3)' }}
            >
              <ArrowLeftRight size={15} />
              Transferir entre contas
            </button>
          )}
          <button
            onClick={() => setShowQuickEntry(true)}
            className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors"
            style={{ background: 'rgba(16,185,129,0.1)', color: 'var(--ff-emerald)', border: '1px solid rgba(16,185,129,0.3)' }}
          >
            <Sparkles size={15} />
            {t('quickEntry.button')}
          </button>
          <button
            onClick={() => setShowForm(true)}
            className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors"
            style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
            onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
            onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
          >
            <Plus size={16} />
            Nova transação
          </button>
        </div>
      </div>

      {/* Pontas de transferência sem par (some quando não há nenhuma) */}
      <PendingTransfersPanel />

      {/* Assinaturas e contas recorrentes detectadas no histórico (some quando não há nenhuma) */}
      <RecurringSuggestionsPanel />

      {/* Filtros */}
      <TransactionFilters
        filters={filters}
        categories={categories}
        onChange={handleFilterChange}
        onClear={() => setFilters(DEFAULT_FILTERS)}
      />

      {/* Skeleton loading em vez de spinner */}
      {isLoading && <SkeletonTable rows={6} columns={5} />}

      {/* Tabela */}
      {!isLoading && !isEmpty && (
        <TransactionTable
          transactions={transactions}
          onEdit={handleEdit}
          onDelete={tx => setDeletingTx(tx)}
          onUnlink={tx => setUnlinkingTx(tx)}
          onShowInstallments={tx => setPlanGroupId(tx.installmentGroupId ?? null)}
          showAccount={accounts.length > 1}
        />
      )}

      {/* EmptyState reutilizável */}
      {isEmpty && (
        <EmptyState
          icon={Receipt}
          title="Nenhuma transação encontrada"
          description="Ainda não tens transações no período seleccionado. Cria a tua primeira receita ou despesa."
          action={{
            label:   'Criar primeira transação',
            onClick: () => setShowForm(true),
          }}
        />
      )}

      {/* Paginação */}
      {!isLoading && totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <button
            onClick={() => handlePageChange(currentPage - 1)}
            disabled={currentPage === 1}
            className="px-3 py-1.5 rounded-lg text-sm transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => {
              e.currentTarget.style.color       = 'var(--ff-text-primary)'
              e.currentTarget.style.background  = 'var(--ff-bg-elevated)'
            }}
            onMouseLeave={e => {
              e.currentTarget.style.color       = 'var(--ff-text-muted)'
              e.currentTarget.style.background  = 'transparent'
            }}
          >
            Anterior
          </button>

          {Array.from({ length: totalPages }, (_, i) => i + 1).map(page => (
            <button
              key={page}
              onClick={() => handlePageChange(page)}
              className="px-3 py-1.5 rounded-lg text-sm font-medium transition-colors"
              style={page === currentPage
                ? { background: 'rgba(16,185,129,0.1)', color: 'var(--ff-emerald)', outline: '1px solid rgba(16,185,129,0.3)' }
                : { color: 'var(--ff-text-muted)' }
              }
              onMouseEnter={e => {
                if (page !== currentPage) {
                  e.currentTarget.style.color       = 'var(--ff-text-primary)'
                  e.currentTarget.style.background  = 'var(--ff-bg-elevated)'
                }
              }}
              onMouseLeave={e => {
                if (page !== currentPage) {
                  e.currentTarget.style.color       = 'var(--ff-text-muted)'
                  e.currentTarget.style.background  = 'transparent'
                }
              }}
            >
              {page}
            </button>
          ))}

          <button
            onClick={() => handlePageChange(currentPage + 1)}
            disabled={currentPage === totalPages}
            className="px-3 py-1.5 rounded-lg text-sm transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => {
              e.currentTarget.style.color       = 'var(--ff-text-primary)'
              e.currentTarget.style.background  = 'var(--ff-bg-elevated)'
            }}
            onMouseLeave={e => {
              e.currentTarget.style.color       = 'var(--ff-text-muted)'
              e.currentTarget.style.background  = 'transparent'
            }}
          >
            Próximo
          </button>
        </div>
      )}

      {showForm && (
        <TransactionForm transaction={editingTx ?? undefined} onClose={handleCloseForm} />
      )}

      {showQuickEntry && (
        <QuickEntryDialog onClose={() => setShowQuickEntry(false)} onResult={handleQuickEntryResult} />
      )}

      {aiBatch && (
        <TransactionForm
          key={aiIndex}
          draft={aiBatch.drafts[aiIndex]}
          draftAttachment={aiBatch.image}
          draftProgress={{ current: aiIndex + 1, total: aiBatch.drafts.length }}
          onDiscardDrafts={() => setAiBatch(null)}
          onClose={handleCloseDraft}
        />
      )}

      {showTransferForm && (
        <TransferFormDialog onClose={() => setShowTransferForm(false)} />
      )}

      {unlinkingTx?.transferLinkId && (
        <UnlinkTransferDialog
          linkId={unlinkingTx.transferLinkId}
          summary={`${unlinkingTx.description || 'Transferência'} · ${unlinkingTx.accountName ?? ''} ↔ ${unlinkingTx.counterpartAccountName ?? ''}`}
          onClose={() => setUnlinkingTx(null)}
        />
      )}

      {planGroupId && (
        <InstallmentPlanDialog groupId={planGroupId} onClose={() => setPlanGroupId(null)} />
      )}

      {deletingTx && (
        <DeleteTransactionDialog transaction={deletingTx} onClose={() => setDeletingTx(null)} />
      )}
    </div>
  )
}