import { useState } from 'react'
import { Plus, Tag, Loader2 } from 'lucide-react'
import { useCategories } from '../api/useCategories'
import CategoryCard from '../components/CategoryCard'
import CategoryModal, { type ModalState } from '../components/CategoryModal'
import { TransactionType } from '../types/category.types'

type Filter = 'all' | 'income' | 'expense'

const FILTERS: { label: string; value: Filter }[] = [
  { label: 'Todas',    value: 'all'     },
  { label: 'Receitas', value: 'income'  },
  { label: 'Despesas', value: 'expense' },
]

export default function CategoriesPage() {
  const [filter, setFilter] = useState<Filter>('all')
  const [modal, setModal]   = useState<ModalState>(null)

  const { data: categories, isLoading } = useCategories()

  const filtered = categories?.filter(c => {
    if (filter === 'income')  return c.type === TransactionType.Income
    if (filter === 'expense') return c.type === TransactionType.Expense
    return true
  }) ?? []

  const isEmpty = !isLoading && filtered.length === 0

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            Categorias
          </h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Organize suas receitas e despesas por categoria
          </p>
        </div>
        <button
          onClick={() => setModal({ type: 'create-category' })}
          className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
          onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
        >
          <Plus size={16} />
          Nova categoria
        </button>
      </div>

      {/* Filtros */}
      <div className="flex gap-2">
        {FILTERS.map(f => (
          <button
            key={f.value}
            onClick={() => setFilter(f.value)}
            className="px-4 py-1.5 rounded-xl text-sm font-medium transition-all duration-200"
            style={filter === f.value
              ? { background: 'rgba(16,185,129,0.1)', color: 'var(--ff-emerald)', outline: '1px solid rgba(16,185,129,0.3)' }
              : { color: 'var(--ff-text-muted)' }
            }
            onMouseEnter={e => {
              if (filter !== f.value) {
                e.currentTarget.style.color = 'var(--ff-text-primary)'
                e.currentTarget.style.background = 'var(--ff-bg-elevated)'
              }
            }}
            onMouseLeave={e => {
              if (filter !== f.value) {
                e.currentTarget.style.color = 'var(--ff-text-muted)'
                e.currentTarget.style.background = 'transparent'
              }
            }}
          >
            {f.label}
          </button>
        ))}
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
        </div>
      )}

      {/* Lista */}
      {!isLoading && !isEmpty && (
        <div className="columns-1 sm:columns-2 lg:columns-3 gap-4">
          {filtered.map(category => (
            <div key={category.id} className="break-inside-avoid mb-4">
              <CategoryCard
                category={category}
                onEdit={cat => setModal({ type: 'edit-category', category: cat })}
                onDelete={cat => setModal({ type: 'delete-category', category: cat })}
                onAddSub={cat => setModal({ type: 'create-subcategory', category: cat })}
                onEditSub={(cat, sub) => setModal({ type: 'edit-subcategory', category: cat, subcategory: sub })}
                onDeleteSub={(cat, sub) => setModal({ type: 'delete-subcategory', category: cat, subcategory: sub })}
              />
            </div>
          ))}
        </div>
      )}

      {/* Estado vazio */}
      {isEmpty && (
        <div className="flex flex-col items-center justify-center py-20 space-y-3">
          <div
            className="w-14 h-14 rounded-2xl flex items-center justify-center"
            style={{ background: 'var(--ff-bg-card)' }}
          >
            <Tag size={24} style={{ color: 'var(--ff-text-muted)' }} />
          </div>
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            {filter === 'all'
              ? 'Nenhuma categoria encontrada'
              : `Nenhuma categoria de ${filter === 'income' ? 'receita' : 'despesa'} encontrada`
            }
          </p>
          <button
            onClick={() => setModal({ type: 'create-category' })}
            className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors"
            style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
            onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
            onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
          >
            <Plus size={15} />
            Criar primeira categoria
          </button>
        </div>
      )}

      <CategoryModal state={modal} onClose={() => setModal(null)} />
    </div>
  )
}