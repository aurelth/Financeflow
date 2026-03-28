import { useEffect, useState } from 'react'
import { X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useCategories } from '../../categories/api/useCategories'
import { useCreateTransaction, useUpdateTransaction } from '../api/useTransactions'
import { TransactionType } from '../../categories/types/category.types'
import {
  TransactionStatus,
  RecurrenceType,
  type Transaction,
  type CreateTransactionRequest,
} from '../types/transaction.types'

interface TransactionFormProps {
  transaction?: Transaction
  onClose:      () => void
}

const defaultForm: CreateTransactionRequest = {
  amount:         0,
  type:           TransactionType.Expense,
  date:           new Date().toISOString().split('T')[0],
  description:    '',
  status:         TransactionStatus.Paid,
  isRecurring:    false,
  recurrenceType: RecurrenceType.None,
  categoryId:     '',
  subcategoryId:  null,
  tags:           [],
}

export default function TransactionForm({ transaction, onClose }: TransactionFormProps) {
  const isEditing = !!transaction

  const [form, setForm]     = useState<CreateTransactionRequest>(
    transaction
      ? {
          amount:         transaction.amount,
          type:           transaction.type,
          date:           transaction.date.split('T')[0],
          description:    transaction.description,
          status:         transaction.status,
          isRecurring:    transaction.isRecurring,
          recurrenceType: transaction.recurrenceType,
          categoryId:     transaction.categoryId,
          subcategoryId:  transaction.subcategoryId,
          tags:           transaction.tags,
        }
      : defaultForm
  )
  const [tagInput, setTagInput] = useState('')

  const { data: categories = [] } = useCategories()
  const createTransaction         = useCreateTransaction()
  const updateTransaction         = useUpdateTransaction(transaction?.id ?? '')

  // Filtra categorias pelo tipo selecionado
  const filteredCategories = categories.filter(c => c.type === form.type)

  // Subcategorias da categoria selecionada
  const selectedCategory   = categories.find(c => c.id === form.categoryId)
  const subcategories      = selectedCategory?.subcategories ?? []

  // Reset categoria ao mudar tipo
  useEffect(() => {
    setForm(f => ({ ...f, categoryId: '', subcategoryId: null }))
  }, [form.type])

  function handleSubmit() {
    if (!form.categoryId || form.amount <= 0) return

    if (isEditing) {
      updateTransaction.mutate(form, { onSuccess: onClose })
    } else {
      createTransaction.mutate(form, { onSuccess: onClose })
    }
  }

  function addTag() {
    const tag = tagInput.trim()
    if (tag && !form.tags.includes(tag)) {
      setForm(f => ({ ...f, tags: [...f.tags, tag] }))
    }
    setTagInput('')
  }

  function removeTag(tag: string) {
    setForm(f => ({ ...f, tags: f.tags.filter(t => t !== tag) }))
  }

  const isPending = createTransaction.isPending || updateTransaction.isPending

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-lg shadow-2xl max-h-[90vh] overflow-y-auto">

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-800">
          <h2 className="text-slate-100 font-semibold text-lg">
            {isEditing ? 'Editar transação' : 'Nova transação'}
          </h2>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-all"
          >
            <X size={18} />
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-5 space-y-4">

          {/* Tipo */}
          <div className="grid grid-cols-2 gap-2">
            {[TransactionType.Expense, TransactionType.Income].map(t => (
              <button
                key={t}
                onClick={() => setForm(f => ({ ...f, type: t }))}
                className={cn(
                  'py-2.5 rounded-xl text-sm font-semibold border transition-all',
                  form.type === t
                    ? t === TransactionType.Income
                      ? 'bg-emerald-500/10 border-emerald-500/40 text-emerald-400'
                      : 'bg-red-500/10 border-red-500/40 text-red-400'
                    : 'bg-slate-800 border-slate-700 text-slate-400 hover:border-slate-600'
                )}
              >
                {t === TransactionType.Income ? '↑ Receita' : '↓ Despesa'}
              </button>
            ))}
          </div>

          {/* Valor */}
          <div>
            <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-1.5 block">
              Valor
            </label>
            <input
              type="number"
              min="0"
              step="0.01"
              value={form.amount || ''}
              onChange={e => setForm(f => ({ ...f, amount: Number(e.target.value) }))}
              placeholder="0,00"
              className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
            />
          </div>

          {/* Descrição */}
          <div>
            <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-1.5 block">
              Descrição
            </label>
            <input
              type="text"
              value={form.description}
              onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
              placeholder="Ex: Almoço, Salário..."
              className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
            />
          </div>

          {/* Data e Status */}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-1.5 block">
                Data
              </label>
              <input
                type="date"
                value={form.date}
                onChange={e => setForm(f => ({ ...f, date: e.target.value }))}
                className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
              />
            </div>
            <div>
              <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-1.5 block">
                Status
              </label>
              <select
                value={form.status}
                onChange={e => setForm(f => ({ ...f, status: Number(e.target.value) as TransactionStatus }))}
                className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
              >
                <option value={TransactionStatus.Paid}>Pago</option>
                <option value={TransactionStatus.Pending}>Pendente</option>
                <option value={TransactionStatus.Scheduled}>Agendado</option>
              </select>
            </div>
          </div>

          {/* Categoria */}
          <div>
            <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-1.5 block">
              Categoria
            </label>
            <select
              value={form.categoryId}
              onChange={e => setForm(f => ({ ...f, categoryId: e.target.value, subcategoryId: null }))}
              className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
            >
              <option value="">Selecionar categoria</option>
              {filteredCategories.map(c => (
                <option key={c.id} value={c.id}>{c.icon} {c.name}</option>
              ))}
            </select>
          </div>

          {/* Subcategoria */}
          {subcategories.length > 0 && (
            <div>
              <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-1.5 block">
                Subcategoria
              </label>
              <select
                value={form.subcategoryId ?? ''}
                onChange={e => setForm(f => ({ ...f, subcategoryId: e.target.value || null }))}
                className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
              >
                <option value="">Nenhuma</option>
                {subcategories.map(s => (
                  <option key={s.id} value={s.id}>{s.name}</option>
                ))}
              </select>
            </div>
          )}

          {/* Recorrência */}
          <div className="space-y-2">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={form.isRecurring}
                onChange={e => setForm(f => ({
                  ...f,
                  isRecurring:    e.target.checked,
                  recurrenceType: e.target.checked ? RecurrenceType.Monthly : RecurrenceType.None,
                }))}
                className="w-4 h-4 rounded border-slate-700 bg-slate-800 text-indigo-500 focus:ring-indigo-500"
              />
              <span className="text-sm text-slate-300">Transação recorrente</span>
            </label>

            {form.isRecurring && (
              <select
                value={form.recurrenceType}
                onChange={e => setForm(f => ({ ...f, recurrenceType: Number(e.target.value) as RecurrenceType }))}
                className="w-full bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
              >
                <option value={RecurrenceType.Daily}>Diária</option>
                <option value={RecurrenceType.Weekly}>Semanal</option>
                <option value={RecurrenceType.Monthly}>Mensal</option>
                <option value={RecurrenceType.Yearly}>Anual</option>
              </select>
            )}
          </div>

          {/* Tags */}
          <div>
            <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-1.5 block">
              Tags
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={tagInput}
                onChange={e => setTagInput(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && (e.preventDefault(), addTag())}
                placeholder="Adicionar tag..."
                className="flex-1 bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
              />
              <button
                onClick={addTag}
                className="px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-xl text-slate-300 text-sm hover:border-indigo-500 transition-colors"
              >
                +
              </button>
            </div>
            {form.tags.length > 0 && (
              <div className="flex flex-wrap gap-1.5 mt-2">
                {form.tags.map(tag => (
                  <span
                    key={tag}
                    className="flex items-center gap-1 text-xs px-2 py-1 rounded-lg bg-slate-800 text-slate-300 border border-slate-700"
                  >
                    {tag}
                    <button
                      onClick={() => removeTag(tag)}
                      className="text-slate-500 hover:text-red-400 transition-colors"
                    >
                      <X size={10} />
                    </button>
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="flex gap-3 px-6 py-4 border-t border-slate-800">
          <button
            onClick={onClose}
            className="flex-1 px-4 py-2.5 rounded-xl border border-slate-700 text-slate-300 text-sm font-medium hover:bg-slate-800 transition-colors"
          >
            Cancelar
          </button>
          <button
            onClick={handleSubmit}
            disabled={isPending || !form.categoryId || form.amount <= 0}
            className="flex-1 px-4 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-semibold transition-colors"
          >
            {isPending ? 'Salvando...' : isEditing ? 'Atualizar' : 'Criar'}
          </button>
        </div>
      </div>
    </div>
  )
}