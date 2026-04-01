import { useState } from 'react'
import { Plus, Receipt, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useTransactions } from '../api/useTransactions'
import { useCategories } from '../../categories/api/useCategories'
import TransactionTable from '../components/TransactionTable'
import TransactionFilters from '../components/TransactionFilters'
import TransactionForm from '../components/TransactionForm'
import DeleteTransactionDialog from '../components/DeleteTransactionDialog'
import ExportButton from '@/features/reports/components/ExportButton'
import type { Transaction, GetTransactionsQuery } from '../types/transaction.types'

function getDefaultFilters(): GetTransactionsQuery {
  const now      = new Date()
  const year     = now.getFullYear()
  const month    = now.getMonth()
  const firstDay = new Date(year, month, 1)
  const lastDay  = new Date(year, month + 1, 0)
  const toDateString = (d: Date) => d.toISOString().split('T')[0]
  return {
    page:     1,
    pageSize: 20,
    dateFrom: toDateString(firstDay),
    dateTo:   toDateString(lastDay),
  }
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

  function handleClearFilters() {
    setFilters(DEFAULT_FILTERS)
  }

  function handlePageChange(page: number) {
    setFilters(f => ({ ...f, page }))
  }

  const isEmpty = !isLoading && transactions.length === 0

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-white">Transações</h1>
          <p className="text-slate-400 text-sm mt-0.5">
            Registre e acompanhe as suas receitas e despesas
          </p>
        </div>
        <div className="flex items-center gap-2">
          <ExportButton defaultMonth={filterMonth} defaultYear={filterYear} />
          <Button
            onClick={() => setShowForm(true)}
            className="bg-indigo-600 hover:bg-indigo-500 text-white h-9 px-4 gap-2"
          >
            <Plus size={16} />
            Nova transação
          </Button>
        </div>
      </div>

      {/* Filtros */}
      <TransactionFilters
        filters={filters}
        categories={categories}
        onChange={handleFilterChange}
        onClear={handleClearFilters}
      />

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin text-indigo-400" />
        </div>
      )}

      {/* Tabela */}
      {!isLoading && !isEmpty && (
        <TransactionTable
          transactions={transactions}
          onEdit={handleEdit}
          onDelete={tx => setDeletingTx(tx)}
        />
      )}

      {/* Estado vazio */}
      {isEmpty && (
        <div className="flex flex-col items-center justify-center py-20 space-y-3">
          <div className="w-14 h-14 bg-slate-800 rounded-2xl flex items-center justify-center">
            <Receipt size={24} className="text-slate-500" />
          </div>
          <p className="text-slate-400 text-sm">Nenhuma transação encontrada</p>
          <Button
            onClick={() => setShowForm(true)}
            className="bg-indigo-600 hover:bg-indigo-500 text-white h-9 px-4 gap-2"
          >
            <Plus size={15} />
            Criar primeira transação
          </Button>
        </div>
      )}

      {/* Paginação */}
      {!isLoading && totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <button
            onClick={() => handlePageChange(currentPage - 1)}
            disabled={currentPage === 1}
            className="px-3 py-1.5 rounded-lg text-sm text-slate-400 hover:text-slate-200 hover:bg-slate-800 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
          >
            Anterior
          </button>
          {Array.from({ length: totalPages }, (_, i) => i + 1).map(page => (
            <button
              key={page}
              onClick={() => handlePageChange(page)}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                page === currentPage
                  ? 'bg-indigo-500/20 text-indigo-400 ring-1 ring-indigo-500/40'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
              }`}
            >
              {page}
            </button>
          ))}
          <button
            onClick={() => handlePageChange(currentPage + 1)}
            disabled={currentPage === totalPages}
            className="px-3 py-1.5 rounded-lg text-sm text-slate-400 hover:text-slate-200 hover:bg-slate-800 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
          >
            Próximo
          </button>
        </div>
      )}

      {/* Modal de criação/edição */}
      {showForm && (
        <TransactionForm
          transaction={editingTx ?? undefined}
          onClose={handleCloseForm}
        />
      )}

      {/* Modal de confirmação de exclusão */}
      {deletingTx && (
        <DeleteTransactionDialog
          transaction={deletingTx}
          onClose={() => setDeletingTx(null)}
        />
      )}
    </div>
  )
}