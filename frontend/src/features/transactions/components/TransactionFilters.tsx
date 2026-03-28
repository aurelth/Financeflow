import { Search, X } from 'lucide-react'
import { TransactionType } from '../../categories/types/category.types'
import { TransactionStatus, type GetTransactionsQuery } from '../types/transaction.types'
import type { Category } from '../../categories/types/category.types'

interface TransactionFiltersProps {
  filters:    GetTransactionsQuery
  categories: Category[]
  onChange:   (filters: GetTransactionsQuery) => void
  onClear:    () => void
}

export default function TransactionFilters({
  filters,
  categories,
  onChange,
  onClear,
}: TransactionFiltersProps) {
  const hasFilters = Object.values(filters).some(v =>
    v !== undefined && v !== '' && v !== 1 && v !== 20
  )

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 space-y-3">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">

        {/* Pesquisa */}
        <div className="relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
          <input
            type="text"
            placeholder="Pesquisar..."
            value={filters.search ?? ''}
            onChange={e => onChange({ ...filters, search: e.target.value || undefined })}
            className="w-full bg-slate-800 border border-slate-700 rounded-lg pl-9 pr-3 py-2 text-sm text-slate-200 placeholder-slate-500 focus:outline-none focus:border-indigo-500 transition-colors"
          />
        </div>

        {/* Tipo */}
        <select
          value={filters.type ?? ''}
          onChange={e => onChange({
            ...filters,
            type: e.target.value ? Number(e.target.value) as TransactionType : undefined
          })}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:border-indigo-500 transition-colors"
        >
          <option value="">Todos os tipos</option>
          <option value={TransactionType.Income}>Receita</option>
          <option value={TransactionType.Expense}>Despesa</option>
        </select>

        {/* Status */}
        <select
          value={filters.status ?? ''}
          onChange={e => onChange({
            ...filters,
            status: e.target.value ? Number(e.target.value) as TransactionStatus : undefined
          })}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:border-indigo-500 transition-colors"
        >
          <option value="">Todos os status</option>
          <option value={TransactionStatus.Paid}>Pago</option>
          <option value={TransactionStatus.Pending}>Pendente</option>
          <option value={TransactionStatus.Scheduled}>Agendado</option>
        </select>

        {/* Categoria */}
        <select
          value={filters.categoryId ?? ''}
          onChange={e => onChange({
            ...filters,
            categoryId: e.target.value || undefined,
            subcategoryId: undefined,
          })}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:border-indigo-500 transition-colors"
        >
          <option value="">Todas as categorias</option>
          {categories.map(c => (
            <option key={c.id} value={c.id}>{c.icon} {c.name}</option>
          ))}
        </select>

        {/* Data início */}
        <input
          type="date"
          value={filters.dateFrom ?? ''}
          onChange={e => onChange({ ...filters, dateFrom: e.target.value || undefined })}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:border-indigo-500 transition-colors"
        />

        {/* Data fim */}
        <input
          type="date"
          value={filters.dateTo ?? ''}
          onChange={e => onChange({ ...filters, dateTo: e.target.value || undefined })}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:border-indigo-500 transition-colors"
        />

        {/* Valor mínimo */}
        <input
          type="number"
          placeholder="Valor mínimo"
          value={filters.amountMin ?? ''}
          onChange={e => onChange({
            ...filters,
            amountMin: e.target.value ? Number(e.target.value) : undefined
          })}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 placeholder-slate-500 focus:outline-none focus:border-indigo-500 transition-colors"
        />

        {/* Valor máximo */}
        <input
          type="number"
          placeholder="Valor máximo"
          value={filters.amountMax ?? ''}
          onChange={e => onChange({
            ...filters,
            amountMax: e.target.value ? Number(e.target.value) : undefined
          })}
          className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 placeholder-slate-500 focus:outline-none focus:border-indigo-500 transition-colors"
        />
      </div>

      {/* Limpar filtros */}
      {hasFilters && (
        <div className="flex justify-end">
          <button
            onClick={onClear}
            className="flex items-center gap-1.5 text-xs text-slate-400 hover:text-slate-200 transition-colors"
          >
            <X size={12} />
            Limpar filtros
          </button>
        </div>
      )}
    </div>
  )
}