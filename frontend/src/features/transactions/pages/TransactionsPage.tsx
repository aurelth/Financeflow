import { useState } from 'react'
import { Plus, Receipt } from 'lucide-react'
import { useTransactions }           from '../api/useTransactions'
import { useCategories }             from '../../categories/api/useCategories'
import TransactionTable              from '../components/TransactionTable'
import TransactionFilters            from '../components/TransactionFilters'
import TransactionForm               from '../components/TransactionForm'
import DeleteTransactionDialog       from '../components/DeleteTransactionDialog'
import ExportButton                  from '@/features/reports/components/ExportButton'
import EmptyState                    from '@/components/ui/EmptyState'
import SkeletonTable                 from '@/components/ui/SkeletonTable'
import type { Transaction, GetTransactionsQuery } from '../types/transaction.types'

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
  const [filters, setFilters]       = useState<GetTransactionsQuery>(DEFAULT_FILTERS)
  const [showForm, setShowForm]     = useState(false)
  const [editingTx, setEditingTx]   = useState<Transaction | null>(null)
  const [deletingTx, setDeletingTx] = useState<Transaction | null>(null)

  const { data, isLoading }       = useTransactions(filters)
  const { data: categories = [] } = useCategories()

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

      {deletingTx && (
        <DeleteTransactionDialog transaction={deletingTx} onClose={() => setDeletingTx(null)} />
      )}
    </div>
  )
}