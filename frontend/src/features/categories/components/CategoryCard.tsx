import { Pencil, Trash2, ChevronDown, ChevronUp, Plus } from 'lucide-react'
import { useState } from 'react'
import { TransactionType, type Category, type Subcategory } from '../types/category.types'
import CategoryIcon from './CategoryIcon'

interface CategoryCardProps {
  category:    Category
  onEdit:      (category: Category) => void
  onDelete:    (category: Category) => void
  onAddSub:    (category: Category) => void
  onEditSub:   (category: Category, subcategory: Subcategory) => void
  onDeleteSub: (category: Category, subcategory: Subcategory) => void
}

export default function CategoryCard({
  category, onEdit, onDelete, onAddSub, onEditSub, onDeleteSub,
}: CategoryCardProps) {
  const [expanded, setExpanded] = useState(false)
  const isIncome = category.type === TransactionType.Income

  return (
    <div
      className="rounded-xl overflow-hidden transition-colors"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--ff-border-subtle)')}
      onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
    >
      {/* Cabeçalho */}
      <div className="flex items-center gap-3 p-4">
        <div
          className="w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0"
          style={{ backgroundColor: `${category.color}20`, border: `1px solid ${category.color}40` }}
        >
          <CategoryIcon icon={category.icon} color={category.color} />
        </div>

        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <p className="font-medium text-sm truncate" style={{ color: 'var(--ff-text-primary)' }}>
              {category.name}
            </p>
            {category.isDefault && (
              <span
                className="text-xs px-1.5 py-0.5 rounded-md flex-shrink-0"
                style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-muted)' }}
              >
                Padrão
              </span>
            )}
          </div>
          <span
            className="text-xs font-medium"
            style={{ color: isIncome ? 'var(--ff-income)' : 'var(--ff-expense)' }}
          >
            {isIncome ? 'Receita' : 'Despesa'}
          </span>
        </div>

        <div className="flex items-center gap-1 flex-shrink-0">
          {category.isOwner && (
            <>
              <button
                onClick={() => onAddSub(category)}
                className="p-1.5 rounded-lg transition-all duration-200"
                style={{ color: 'var(--ff-text-muted)' }}
                onMouseEnter={e => {
                  e.currentTarget.style.color = 'var(--ff-emerald)'
                  e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                }}
                onMouseLeave={e => {
                  e.currentTarget.style.color = 'var(--ff-text-muted)'
                  e.currentTarget.style.background = 'transparent'
                }}
                title="Adicionar subcategoria"
              >
                <Plus size={15} />
              </button>
              <button
                onClick={() => onEdit(category)}
                className="p-1.5 rounded-lg transition-all duration-200"
                style={{ color: 'var(--ff-text-muted)' }}
                onMouseEnter={e => {
                  e.currentTarget.style.color = 'var(--ff-text-primary)'
                  e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                }}
                onMouseLeave={e => {
                  e.currentTarget.style.color = 'var(--ff-text-muted)'
                  e.currentTarget.style.background = 'transparent'
                }}
                title="Editar"
              >
                <Pencil size={15} />
              </button>
              <button
                onClick={() => onDelete(category)}
                className="p-1.5 rounded-lg transition-all duration-200"
                style={{ color: 'var(--ff-text-muted)' }}
                onMouseEnter={e => {
                  e.currentTarget.style.color = 'var(--ff-expense)'
                  e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                }}
                onMouseLeave={e => {
                  e.currentTarget.style.color = 'var(--ff-text-muted)'
                  e.currentTarget.style.background = 'transparent'
                }}
                title="Remover"
              >
                <Trash2 size={15} />
              </button>
            </>
          )}

          {category.subcategories.length > 0 && (
            <button
              onClick={() => setExpanded(p => !p)}
              className="p-1.5 rounded-lg transition-all duration-200"
              style={{ color: 'var(--ff-text-muted)' }}
              onMouseEnter={e => {
                e.currentTarget.style.color = 'var(--ff-text-primary)'
                e.currentTarget.style.background = 'var(--ff-bg-elevated)'
              }}
              onMouseLeave={e => {
                e.currentTarget.style.color = 'var(--ff-text-muted)'
                e.currentTarget.style.background = 'transparent'
              }}
            >
              {expanded ? <ChevronUp size={15} /> : <ChevronDown size={15} />}
            </button>
          )}
        </div>
      </div>

      {/* Subcategorias expandidas */}
      {expanded && category.subcategories.length > 0 && (
        <div
          className="px-4 py-2 space-y-1"
          style={{ borderTop: '1px solid var(--ff-border)' }}
        >
          {category.subcategories.map(sub => (
            <div key={sub.id} className="flex items-center gap-2 py-1.5 group/sub">
              <div
                className="w-1.5 h-1.5 rounded-full flex-shrink-0"
                style={{ backgroundColor: category.color }}
              />
              <span className="text-sm flex-1 truncate" style={{ color: 'var(--ff-text-muted)' }}>
                {sub.name}
              </span>

              {category.isOwner && (
                <div className="flex items-center gap-1 opacity-0 group-hover/sub:opacity-100 transition-opacity duration-200">
                  <button
                    onClick={() => onEditSub(category, sub)}
                    className="p-1 rounded-md transition-all duration-200"
                    style={{ color: 'var(--ff-text-muted)' }}
                    onMouseEnter={e => {
                      e.currentTarget.style.color = 'var(--ff-text-primary)'
                      e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                    }}
                    onMouseLeave={e => {
                      e.currentTarget.style.color = 'var(--ff-text-muted)'
                      e.currentTarget.style.background = 'transparent'
                    }}
                    title="Editar subcategoria"
                  >
                    <Pencil size={12} />
                  </button>
                  <button
                    onClick={() => onDeleteSub(category, sub)}
                    className="p-1 rounded-md transition-all duration-200"
                    style={{ color: 'var(--ff-text-muted)' }}
                    onMouseEnter={e => {
                      e.currentTarget.style.color = 'var(--ff-expense)'
                      e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                    }}
                    onMouseLeave={e => {
                      e.currentTarget.style.color = 'var(--ff-text-muted)'
                      e.currentTarget.style.background = 'transparent'
                    }}
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