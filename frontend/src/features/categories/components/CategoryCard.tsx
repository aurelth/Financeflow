import { Pencil, Trash2, ChevronDown, ChevronUp, Plus } from 'lucide-react'
import { useState } from 'react'
import { cn } from '@/lib/utils'
import { TransactionType, type Category, type Subcategory } from '../types/category.types'
import CategoryIcon from './CategoryIcon'

interface CategoryCardProps {
  category:        Category
  onEdit:          (category: Category) => void
  onDelete:        (category: Category) => void
  onAddSub:        (category: Category) => void
  onEditSub:       (category: Category, subcategory: Subcategory) => void
  onDeleteSub:     (category: Category, subcategory: Subcategory) => void
}

export default function CategoryCard({
  category,
  onEdit,
  onDelete,
  onAddSub,
  onEditSub,
  onDeleteSub,
}: CategoryCardProps) {
  const [expanded, setExpanded] = useState(false)

  const isIncome = category.type === TransactionType.Income

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden">

      {/* Cabeçalho */}
      <div className="flex items-center gap-3 p-4">

        {/* Ícone */}
        <div
          className="w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0"
          style={{ backgroundColor: `${category.color}20`, border: `1px solid ${category.color}40` }}
        >
          <CategoryIcon icon={category.icon} color={category.color} />
        </div>

        {/* Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <p className="text-slate-200 font-medium text-sm truncate">{category.name}</p>
            {category.isDefault && (
              <span className="text-xs px-1.5 py-0.5 rounded-md bg-slate-800 text-slate-400 flex-shrink-0">
                Padrão
              </span>
            )}
          </div>
          <span className={cn('text-xs font-medium', isIncome ? 'text-emerald-400' : 'text-red-400')}>
            {isIncome ? 'Receita' : 'Despesa'}
          </span>
        </div>

        {/* Ações da categoria */}
        <div className="flex items-center gap-1 flex-shrink-0">
          {category.isOwner && (
            <>
              <button
                onClick={() => onAddSub(category)}
                className="p-1.5 text-slate-400 hover:text-indigo-400 hover:bg-slate-800 rounded-lg transition-all duration-200"
                title="Adicionar subcategoria"
              >
                <Plus size={15} />
              </button>
              <button
                onClick={() => onEdit(category)}
                className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-all duration-200"
                title="Editar"
              >
                <Pencil size={15} />
              </button>
              <button
                onClick={() => onDelete(category)}
                className="p-1.5 text-slate-400 hover:text-red-400 hover:bg-slate-800 rounded-lg transition-all duration-200"
                title="Remover"
              >
                <Trash2 size={15} />
              </button>
            </>
          )}

          {category.subcategories.length > 0 && (
            <button
              onClick={() => setExpanded(p => !p)}
              className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-all duration-200"
            >
              {expanded ? <ChevronUp size={15} /> : <ChevronDown size={15} />}
            </button>
          )}
        </div>
      </div>

      {/* Subcategorias expandidas */}
      {expanded && category.subcategories.length > 0 && (
        <div className="border-t border-slate-800 px-4 py-2 space-y-1">
          {category.subcategories.map(sub => (
            <div
              key={sub.id}
              className="flex items-center gap-2 py-1.5 group/sub"
            >
              <div
                className="w-1.5 h-1.5 rounded-full flex-shrink-0"
                style={{ backgroundColor: category.color }}
              />
              <span className="text-sm text-slate-400 flex-1 truncate">{sub.name}</span>

              {/* Ações inline da subcategoria — visíveis no hover */}
              {category.isOwner && (
                <div className="flex items-center gap-1 opacity-0 group-hover/sub:opacity-100 transition-opacity duration-200">
                  <button
                    onClick={() => onEditSub(category, sub)}
                    className="p-1 text-slate-500 hover:text-slate-200 hover:bg-slate-800 rounded-md transition-all duration-200"
                    title="Editar subcategoria"
                  >
                    <Pencil size={12} />
                  </button>
                  <button
                    onClick={() => onDeleteSub(category, sub)}
                    className="p-1 text-slate-500 hover:text-red-400 hover:bg-slate-800 rounded-md transition-all duration-200"
                    title="Remover subcategoria"
                  >
                    <Trash2 size={12} />
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}