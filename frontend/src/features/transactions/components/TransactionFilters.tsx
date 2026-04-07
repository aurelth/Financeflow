import { Search, X } from 'lucide-react'
import { TransactionType } from '../../categories/types/category.types'
import { TransactionStatus, type GetTransactionsQuery } from '../types/transaction.types'
import type { Category } from '../../categories/types/category.types'
import CategorySelect from './CategorySelect'

interface TransactionFiltersProps {
  filters:    GetTransactionsQuery
  categories: Category[]
  onChange:   (filters: GetTransactionsQuery) => void
  onClear:    () => void
}

// Estilos inline dos inputs com tokens
const inputStyle: React.CSSProperties = {
  background:  'var(--ff-bg-elevated)',
  border:      '1px solid var(--ff-border)',
  color:       'var(--ff-text-primary)',
  borderRadius: '8px',
  padding:     '8px 12px',
  fontSize:    '13px',
  width:       '100%',
  outline:     'none',
  transition:  'border-color 0.15s',
}

export default function TransactionFilters({ filters, categories, onChange, onClear }: TransactionFiltersProps) {
  const hasFilters = Object.values(filters).some(v =>
    v !== undefined && v !== '' && v !== 1 && v !== 20
  )

  return (
    <div
      className="rounded-xl p-4 space-y-3"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">

        {/* Pesquisa */}
        <div className="relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: 'var(--ff-text-muted)' }} />
          <input
            type="text"
            placeholder="Pesquisar..."
            value={filters.search ?? ''}
            onChange={e => onChange({ ...filters, search: e.target.value || undefined })}
            style={{ ...inputStyle, paddingLeft: '34px' }}
            onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
            onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
          />
        </div>

        {/* Tipo */}
        <select
          value={filters.type ?? ''}
          onChange={e => onChange({ ...filters, type: e.target.value ? Number(e.target.value) as TransactionType : undefined })}
          style={inputStyle}
          onFocus={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
        >
          <option value="">Todos os tipos</option>
          <option value={TransactionType.Income}>Receita</option>
          <option value={TransactionType.Expense}>Despesa</option>
        </select>

        {/* Status */}
        <select
          value={filters.status ?? ''}
          onChange={e => onChange({ ...filters, status: e.target.value ? Number(e.target.value) as TransactionStatus : undefined })}
          style={inputStyle}
          onFocus={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
        >
          <option value="">Todos os status</option>
          <option value={TransactionStatus.Paid}>Pago</option>
          <option value={TransactionStatus.Pending}>Pendente</option>
          <option value={TransactionStatus.Scheduled}>Agendado</option>
        </select>

        {/* Categoria */}
        <CategorySelect
          categories={categories}
          value={filters.categoryId ?? ''}
          onChange={categoryId => onChange({ ...filters, categoryId: categoryId || undefined, subcategoryId: undefined })}
          nullable
          nullLabel="Todas as categorias"
        />

        {/* Data início */}
        <input
          type="date"
          value={filters.dateFrom ?? ''}
          onChange={e => onChange({ ...filters, dateFrom: e.target.value || undefined })}
          style={inputStyle}
          onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
        />

        {/* Data fim */}
        <input
          type="date"
          value={filters.dateTo ?? ''}
          onChange={e => onChange({ ...filters, dateTo: e.target.value || undefined })}
          style={inputStyle}
          onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
        />

        {/* Valor mínimo */}
        <input
          type="number"
          placeholder="Valor mínimo"
          value={filters.amountMin ?? ''}
          onChange={e => onChange({ ...filters, amountMin: e.target.value ? Number(e.target.value) : undefined })}
          style={inputStyle}
          onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
        />

        {/* Valor máximo */}
        <input
          type="number"
          placeholder="Valor máximo"
          value={filters.amountMax ?? ''}
          onChange={e => onChange({ ...filters, amountMax: e.target.value ? Number(e.target.value) : undefined })}
          style={inputStyle}
          onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
          onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
        />
      </div>

      {/* Limpar filtros */}
      {hasFilters && (
        <div className="flex justify-end">
          <button
            onClick={onClear}
            className="flex items-center gap-1.5 text-xs transition-colors"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-text-primary)')}
            onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
          >
            <X size={12} />
            Limpar filtros
          </button>
        </div>
      )}
    </div>
  )
}