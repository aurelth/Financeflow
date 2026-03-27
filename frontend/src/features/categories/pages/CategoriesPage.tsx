import { useState } from 'react'
import { Plus, Tag, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { useCategories } from '../api/useCategories'
import CategoryCard from '../components/CategoryCard'
import CategoryModal, { type ModalState } from '../components/CategoryModal'
import { TransactionType, type Category, type Subcategory } from '../types/category.types'

// Filtros

type Filter = 'all' | 'income' | 'expense'

const FILTERS: { label: string; value: Filter }[] = [
  { label: 'Todas',    value: 'all'     },
  { label: 'Receitas', value: 'income'  },
  { label: 'Despesas', value: 'expense' },
]

export default function CategoriesPage() {
  const [filter, setFilter] = useState<Filter>('all')
  const [modal, setModal] = useState<ModalState>(null)

  const { data: categories, isLoading } = useCategories()

  // Filtragem

  const filtered = categories?.filter(c => {
    if (filter === 'income')  return c.type === TransactionType.Income
    if (filter === 'expense') return c.type === TransactionType.Expense
    return true
  }) ?? []

  // Handlers de modal

  const openCreateCategory = () =>
    setModal({ type: 'create-category' })

  const openEditCategory = (category: Category) =>
    setModal({ type: 'edit-category', category })

  const openDeleteCategory = (category: Category) =>
    setModal({ type: 'delete-category', category })

  const openAddSubcategory = (category: Category) =>
    setModal({ type: 'create-subcategory', category })

  const openEditSubcategory = (category: Category, subcategory: Subcategory) =>
    setModal({ type: 'edit-subcategory', category, subcategory })

  const openDeleteSubcategory = (category: Category, subcategory: Subcategory) =>
    setModal({ type: 'delete-subcategory', category, subcategory })

  // Estados visuais

  const isEmpty = !isLoading && filtered.length === 0

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-white">Categorias</h1>
          <p className="text-slate-400 text-sm mt-0.5">
            Organize suas receitas e despesas por categoria
          </p>
        </div>
        <Button
          onClick={openCreateCategory}
          className="bg-indigo-600 hover:bg-indigo-500 text-white h-9 px-4 gap-2"
        >
          <Plus size={16} />
          Nova categoria
        </Button>
      </div>

      {/* Filtros */}
      <div className="flex gap-2">
        {FILTERS.map(f => (
          <button
            key={f.value}
            onClick={() => setFilter(f.value)}
            className={cn(
              'px-4 py-1.5 rounded-xl text-sm font-medium transition-all duration-200',
              filter === f.value
                ? 'bg-indigo-500/20 text-indigo-400 ring-1 ring-indigo-500/40'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
            )}
          >
            {f.label}
          </button>
        ))}
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin text-indigo-400" />
        </div>
      )}

      {/* Lista */}
      {!isLoading && !isEmpty && (
        <div className="columns-1 sm:columns-2 lg:columns-3 gap-4">
          {filtered.map(category => (
            <div key={category.id} className="break-inside-avoid mb-4">
              <CategoryCard            
              category={category}
              onEdit={openEditCategory}
              onDelete={openDeleteCategory}
              onAddSub={openAddSubcategory}
              onEditSub={openEditSubcategory}
              onDeleteSub={openDeleteSubcategory}
              />
            </div>            
          ))}
        </div>
      )}

      {/* Estado vazio */}
      {isEmpty && (
        <div className="flex flex-col items-center justify-center py-20 space-y-3">
          <div className="w-14 h-14 bg-slate-800 rounded-2xl flex items-center justify-center">
            <Tag size={24} className="text-slate-500" />
          </div>
          <p className="text-slate-400 text-sm">
            {filter === 'all'
              ? 'Nenhuma categoria encontrada'
              : `Nenhuma categoria de ${filter === 'income' ? 'receita' : 'despesa'} encontrada`
            }
          </p>
          <Button
            onClick={openCreateCategory}
            className="bg-indigo-600 hover:bg-indigo-500 text-white h-9 px-4 gap-2"
          >
            <Plus size={15} />
            Criar primeira categoria
          </Button>
        </div>
      )}

      {/* Modal */}
      <CategoryModal
        state={modal}
        onClose={() => setModal(null)}
      />
    </div>
  )
}