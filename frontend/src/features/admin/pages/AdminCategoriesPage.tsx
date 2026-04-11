import { useState } from 'react'
import { Plus, Pencil, Trash2, Loader2 } from 'lucide-react'
import {
  useAdminCategories,
  useCreateDefaultCategory,
  useUpdateDefaultCategory,
  useDeleteDefaultCategory,
} from '../api/useAdmin'
import type { AdminCategory, CreateDefaultCategoryRequest, UpdateDefaultCategoryRequest } from '../types/admin.types'

const ICONS = [
  'utensils', 'car', 'heart-pulse', 'house', 'graduation-cap',
  'gamepad-2', 'shirt', 'monitor', 'ellipsis', 'briefcase',
  'laptop', 'trending-up', 'star', 'shopping-cart', 'music',
  'plane', 'coffee', 'dumbbell', 'pet', 'gift',
]

const inputStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  borderRadius: '10px',
  padding:      '9px 14px',
  color:        'var(--ff-text-primary)',
  fontSize:     '14px',
  outline:      'none',
}

const labelStyle: React.CSSProperties = {
  display:      'block',
  fontSize:     '12px',
  fontWeight:   500,
  color:        'var(--ff-text-muted)',
  marginBottom: '4px',
}

type FormMode = 'create' | 'edit'

interface CategoryFormState {
  name:  string
  icon:  string
  color: string
  type:  'Expense' | 'Income'
}

const defaultForm: CategoryFormState = {
  name:  '',
  icon:  'ellipsis',
  color: '#10b981',
  type:  'Expense',
}

export default function AdminCategoriesPage() {
  const [showForm,    setShowForm]    = useState(false)
  const [formMode,    setFormMode]    = useState<FormMode>('create')
  const [editingId,   setEditingId]   = useState<string | null>(null)
  const [form,        setForm]        = useState<CategoryFormState>(defaultForm)
  const [deleteTarget, setDeleteTarget] = useState<AdminCategory | null>(null)
  const [filterType,  setFilterType]  = useState<'All' | 'Expense' | 'Income'>('All')

  const { data: categories, isLoading } = useAdminCategories()
  const createCategory = useCreateDefaultCategory()
  const updateCategory = useUpdateDefaultCategory()
  const deleteCategory = useDeleteDefaultCategory()

  const filtered = categories?.filter(c =>
    filterType === 'All' ? true : c.type === filterType
  )

  function openCreate() {
    setForm(defaultForm)
    setFormMode('create')
    setEditingId(null)
    setShowForm(true)
  }

  function openEdit(cat: AdminCategory) {
    setForm({ name: cat.name, icon: cat.icon, color: cat.color, type: cat.type })
    setFormMode('edit')
    setEditingId(cat.id)
    setShowForm(true)
  }

  function handleSubmit() {
    if (!form.name.trim()) return

    if (formMode === 'create') {
      createCategory.mutate(form as CreateDefaultCategoryRequest, {
        onSuccess: () => setShowForm(false),
      })
    } else if (editingId) {
      updateCategory.mutate(
        { id: editingId, ...form } as UpdateDefaultCategoryRequest & { id: string },
        { onSuccess: () => setShowForm(false) }
      )
    }
  }

  function handleDelete() {
    if (!deleteTarget) return
    deleteCategory.mutate(deleteTarget.id, {
      onSuccess: () => setDeleteTarget(null),
    })
  }

  const isPending =
    createCategory.isPending ||
    updateCategory.isPending ||
    deleteCategory.isPending

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>
            Categorias Padrão
          </h1>
          <p className="text-sm mt-1" style={{ color: 'var(--ff-text-muted)' }}>
            Gerencie as categorias padrão disponíveis para todos os usuários
          </p>
        </div>
        <button
          onClick={openCreate}
          className="flex items-center gap-2 px-4 h-10 rounded-xl text-sm font-medium transition-colors"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
          onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
        >
          <Plus size={16} />
          Nova categoria
        </button>
      </div>

      {/* Filtro de tipo */}
      <div className="flex gap-2">
        {(['All', 'Expense', 'Income'] as const).map(type => {
          const labels = { All: 'Todas', Expense: 'Despesas', Income: 'Receitas' }
          const active = filterType === type
          return (
            <button
              key={type}
              onClick={() => setFilterType(type)}
              className="px-4 py-2 rounded-xl text-sm font-medium transition-colors"
              style={{
                background: active ? 'var(--ff-emerald-subtle)' : 'var(--ff-bg-elevated)',
                color:      active ? 'var(--ff-emerald)'         : 'var(--ff-text-muted)',
                border:     active ? '1px solid rgba(16,185,129,0.3)' : '1px solid var(--ff-border)',
              }}
            >
              {labels[type]}
            </button>
          )
        })}
      </div>

      {/* Lista */}
      <div
        className="rounded-2xl overflow-hidden"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        {isLoading ? (
          <div className="flex items-center justify-center py-20">
            <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr style={{ borderBottom: '1px solid var(--ff-border)' }}>
                {['Ícone', 'Nome', 'Tipo', 'Criado em', 'Ações'].map(h => (
                  <th
                    key={h}
                    className="text-left px-4 py-3 font-medium text-xs uppercase tracking-wide"
                    style={{ color: 'var(--ff-text-muted)' }}
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {filtered?.map(cat => (
                <tr key={cat.id} style={{ borderBottom: '1px solid var(--ff-border-subtle)' }}>
                  <td className="px-4 py-3">
                    <div
                      className="w-8 h-8 rounded-lg flex items-center justify-center text-sm"
                      style={{ background: `${cat.color}22`, color: cat.color }}
                    >
                      {cat.icon}
                    </div>
                  </td>
                  <td className="px-4 py-3 font-medium" style={{ color: 'var(--ff-text-primary)' }}>
                    {cat.name}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className="px-2 py-0.5 rounded-full text-xs font-medium"
                      style={cat.type === 'Expense'
                        ? { background: 'rgba(244,63,94,0.1)',  color: 'var(--ff-expense)', border: '1px solid rgba(244,63,94,0.2)'  }
                        : { background: 'rgba(34,197,94,0.1)',  color: 'var(--ff-income)',  border: '1px solid rgba(34,197,94,0.2)'  }
                      }
                    >
                      {cat.type === 'Expense' ? 'Despesa' : 'Receita'}
                    </span>
                  </td>
                  <td className="px-4 py-3" style={{ color: 'var(--ff-text-muted)' }}>
                    {new Date(cat.createdAt).toLocaleDateString('pt-BR')}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1">
                      <button
                        onClick={() => openEdit(cat)}
                        className="p-1.5 rounded-lg transition-colors"
                        title="Editar"
                        style={{ color: 'var(--ff-text-muted)' }}
                        onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
                        onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                      >
                        <Pencil size={15} />
                      </button>
                      <button
                        onClick={() => setDeleteTarget(cat)}
                        className="p-1.5 rounded-lg transition-colors"
                        title="Excluir"
                        style={{ color: 'var(--ff-text-muted)' }}
                        onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-expense)')}
                        onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Modal de formulário */}
      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
          <div
            className="w-full max-w-md rounded-2xl shadow-2xl"
            style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
          >
            <div className="px-6 py-4" style={{ borderBottom: '1px solid var(--ff-border)' }}>
              <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
                {formMode === 'create' ? 'Nova categoria padrão' : 'Editar categoria padrão'}
              </h2>
            </div>

            <div className="px-6 py-5 space-y-4">
              <div>
                <label style={labelStyle}>Nome</label>
                <input
                  id="category-name"
                  value={form.name}
                  onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                  placeholder="Nome da categoria"
                  style={inputStyle}
                  onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
                  onBlur={e  => (e.target.style.borderColor = 'var(--ff-border)')}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label style={labelStyle}>Cor</label>
                  <div className="flex items-center gap-2">
                    <input
                      type="color"
                      value={form.color}
                      onChange={e => setForm(f => ({ ...f, color: e.target.value }))}
                      className="w-10 h-10 rounded-lg cursor-pointer border-0"
                      style={{ background: 'transparent' }}
                    />
                    <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                      {form.color}
                    </span>
                  </div>
                </div>

                {formMode === 'create' && (
                  <div>
                    <label style={labelStyle}>Tipo</label>
                    <select
                      value={form.type}
                      onChange={e => setForm(f => ({ ...f, type: e.target.value as 'Expense' | 'Income' }))}
                      style={inputStyle}
                      onFocus={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
                      onBlur={e  => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
                    >
                      <option value="Expense" style={{ background: '#111' }}>Despesa</option>
                      <option value="Income"  style={{ background: '#111' }}>Receita</option>
                    </select>
                  </div>
                )}
              </div>

              <div>
                <label style={labelStyle}>Ícone</label>
                <div className="grid grid-cols-5 gap-2">
                  {ICONS.map(icon => (
                    <button
                      key={icon}
                      type="button"
                      onClick={() => setForm(f => ({ ...f, icon }))}
                      className="h-10 rounded-lg text-xs transition-colors"
                      style={{
                        background: form.icon === icon ? 'var(--ff-emerald-subtle)' : 'var(--ff-bg-elevated)',
                        color:      form.icon === icon ? 'var(--ff-emerald)'         : 'var(--ff-text-muted)',
                        border:     form.icon === icon ? '1px solid rgba(16,185,129,0.3)' : '1px solid var(--ff-border)',
                      }}
                    >
                      {icon}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="flex gap-3 px-6 py-4" style={{ borderTop: '1px solid var(--ff-border)' }}>
              <button
                onClick={() => setShowForm(false)}
                disabled={isPending}
                className="flex-1 h-10 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
                style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-secondary)', border: '1px solid var(--ff-border)' }}
              >
                Cancelar
              </button>
              <button
                onClick={handleSubmit}
                disabled={isPending || !form.name.trim()}
                className="flex-1 h-10 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
                style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
                onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)' }}
                onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald)' }}
              >
                {isPending
                  ? <><Loader2 size={14} className="animate-spin" />Salvando...</>
                  : formMode === 'create' ? 'Criar categoria' : 'Salvar alterações'
                }
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal de confirmação de exclusão */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
          <div
            className="w-full max-w-md rounded-2xl shadow-2xl"
            style={{ background: 'var(--ff-bg-card)', border: '1px solid rgba(244,63,94,0.3)' }}
          >
            <div className="px-6 py-4" style={{ borderBottom: '1px solid var(--ff-border)' }}>
              <h2 className="font-semibold" style={{ color: 'var(--ff-expense)' }}>
                Excluir categoria padrão
              </h2>
            </div>
            <div className="px-6 py-5">
              <p className="text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
                Deseja excluir a categoria <strong style={{ color: 'var(--ff-text-primary)' }}>{deleteTarget.name}</strong>?
                Esta ação não pode ser desfeita.
              </p>
            </div>
            <div className="flex gap-3 px-6 py-4" style={{ borderTop: '1px solid var(--ff-border)' }}>
              <button
                onClick={() => setDeleteTarget(null)}
                disabled={isPending}
                className="flex-1 h-10 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
                style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-secondary)', border: '1px solid var(--ff-border)' }}
              >
                Cancelar
              </button>
              <button
                onClick={handleDelete}
                disabled={isPending}
                className="flex-1 h-10 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
                style={{ background: 'var(--ff-expense)', color: '#fff' }}
                onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = '#e11d48' }}
                onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-expense)' }}
              >
                {isPending
                  ? <><Loader2 size={14} className="animate-spin" />Excluindo...</>
                  : 'Excluir categoria'
                }
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}