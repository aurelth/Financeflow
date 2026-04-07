import { useEffect, useRef, useState } from 'react'
import { X, Paperclip, FileText, Image, RefreshCw } from 'lucide-react'
import AttachmentViewer from './AttachmentViewer'
import { useCategories } from '../../categories/api/useCategories'
import {
  useCreateTransaction,
  useUpdateTransaction,
  useUploadAttachment,
  useRemoveAttachment,
} from '../api/useTransactions'
import { TransactionType } from '../../categories/types/category.types'
import CategorySelect from './CategorySelect'
import {
  TransactionStatus,
  RecurrenceType,
  type Transaction,
  type CreateTransactionRequest,
  type UpdateTransactionRequest,
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

// Estilos de input com tokens da nova paleta
const inputStyle: React.CSSProperties = {
  width:        '100%',
  background:   'var(--ff-bg-elevated)',
  border:       '1px solid var(--ff-border)',
  borderRadius: '12px',
  padding:      '10px 16px',
  color:        'var(--ff-text-primary)',
  fontSize:     '14px',
  outline:      'none',
  transition:   'border-color 0.15s',
}

const labelStyle: React.CSSProperties = {
  display:       'block',
  fontSize:      '11px',
  fontWeight:    500,
  textTransform: 'uppercase',
  letterSpacing: '0.06em',
  color:         'var(--ff-text-muted)',
  marginBottom:  '6px',
}

export default function TransactionForm({ transaction, onClose }: TransactionFormProps) {
  const isEditing = !!transaction

  const [form, setForm] = useState<CreateTransactionRequest>(
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

  const [tagInput, setTagInput]   = useState('')
  const [attachment, setAttachment] = useState<File | null>(null)
  const [currentAttachmentPath, setCurrentAttachmentPath] = useState<string | null>(transaction?.attachmentPath ?? null)
  const [currentAttachmentName, setCurrentAttachmentName] = useState<string | null>(transaction?.attachmentName ?? null)
  const [showPropagateModal, setShowPropagateModal] = useState(false)

  const attachmentInputRef = useRef<HTMLInputElement>(null)

  const { data: categories = [] } = useCategories()
  const createTransaction         = useCreateTransaction()
  const updateTransaction         = useUpdateTransaction(transaction?.id ?? '')
  const uploadAttachment          = useUploadAttachment(transaction?.id ?? '')
  const removeAttachment          = useRemoveAttachment(transaction?.id ?? '')

  const filteredCategories = categories.filter(c => c.type === form.type)
  const selectedCategory   = categories.find(c => c.id === form.categoryId)
  const subcategories      = selectedCategory?.subcategories ?? []
  const initialType        = useRef(form.type)

  useEffect(() => {
    if (form.type === initialType.current) return
    setForm(f => ({ ...f, categoryId: '', subcategoryId: null }))
  }, [form.type])

  const amountAlterado       = transaction && form.amount        !== transaction.amount
  const descricaoAlterada    = transaction && form.description   !== transaction.description
  const categoriaAlterada    = transaction && form.categoryId    !== transaction.categoryId
  const subcategoriaAlterada = transaction && form.subcategoryId !== transaction.subcategoryId

  const devePerguntar = isEditing
    && transaction?.recurrenceGroupId
    && (amountAlterado || descricaoAlterada || categoriaAlterada || subcategoriaAlterada)

  function handleSubmit() {
    if (!form.categoryId || form.amount <= 0) return
    if (isEditing && devePerguntar) { setShowPropagateModal(true); return }
    executarUpdate(false)
  }

  function executarUpdate(propagateToFuture: boolean) {
    setShowPropagateModal(false)
    const updateData: UpdateTransactionRequest = { ...form, propagateToFuture }
    updateTransaction.mutate(updateData, {
      onSuccess: () => {
        if (attachment) {
          uploadAttachment.mutate(attachment, {
            onSuccess: updated => {
              setCurrentAttachmentPath(updated.attachmentPath)
              setCurrentAttachmentName(updated.attachmentName ?? null)
              setAttachment(null)
              onClose()
            }
          })
        } else {
          onClose()
        }
      },
    })
  }

  function handleCreate() {
    createTransaction.mutate(
      { data: form, attachment: attachment ?? undefined },
      { onSuccess: onClose }
    )
  }

  function addTag() {
    const tag = tagInput.trim()
    if (tag && !form.tags.includes(tag)) setForm(f => ({ ...f, tags: [...f.tags, tag] }))
    setTagInput('')
  }

  function removeTag(tag: string) {
    setForm(f => ({ ...f, tags: f.tags.filter(t => t !== tag) }))
  }

  const isPending =
    createTransaction.isPending || updateTransaction.isPending ||
    uploadAttachment.isPending  || removeAttachment.isPending

  return (
    <>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
        <div
          className="w-full max-w-lg rounded-2xl shadow-2xl max-h-[90vh] overflow-y-auto"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          {/* Header */}
          <div
            className="flex items-center justify-between px-6 py-4"
            style={{ borderBottom: '1px solid var(--ff-border)' }}
          >
            <h2 className="font-semibold text-lg" style={{ color: 'var(--ff-text-primary)' }}>
              {isEditing ? 'Editar transação' : 'Nova transação'}
            </h2>
            <button
              onClick={onClose}
              className="p-1.5 rounded-lg transition-all"
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
                  className="py-2.5 rounded-xl text-sm font-semibold transition-all"
                  style={form.type === t
                    ? t === TransactionType.Income
                      ? { background: 'rgba(16,185,129,0.1)', border: '1px solid rgba(16,185,129,0.4)', color: 'var(--ff-income)' }
                      : { background: 'rgba(244,63,94,0.1)', border: '1px solid rgba(244,63,94,0.4)', color: 'var(--ff-expense)' }
                    : { background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-muted)' }
                  }
                >
                  {t === TransactionType.Income ? '↑ Receita' : '↓ Despesa'}
                </button>
              ))}
            </div>

            {/* Valor */}
            <div>
              <label style={labelStyle}>Valor</label>
              <input
                type="number" min="0" step="0.01"
                value={form.amount || ''}
                onChange={e => setForm(f => ({ ...f, amount: Number(e.target.value) }))}
                placeholder="0,00"
                style={inputStyle}
                onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
                onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
              />
            </div>

            {/* Descrição */}
            <div>
              <label style={labelStyle}>Descrição</label>
              <input
                type="text"
                value={form.description}
                onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                placeholder="Ex: Almoço, Salário..."
                style={inputStyle}
                onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
                onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
              />
            </div>

            {/* Data e Status */}
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label style={labelStyle}>Data</label>
                <input
                  type="date"
                  value={form.date}
                  onChange={e => setForm(f => ({ ...f, date: e.target.value }))}
                  style={inputStyle}
                  onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
                  onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
                />
              </div>
              <div>
                <label style={labelStyle}>Status</label>
                <select
                  value={form.status}
                  onChange={e => setForm(f => ({ ...f, status: Number(e.target.value) as TransactionStatus }))}
                  style={inputStyle}
                  onFocus={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
                  onBlur={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
                >
                  <option value={TransactionStatus.Paid}>Pago</option>
                  <option value={TransactionStatus.Pending}>Pendente</option>
                  <option value={TransactionStatus.Scheduled}>Agendado</option>
                </select>
              </div>
            </div>

            {/* Categoria */}
            <div>
              <label style={labelStyle}>Categoria</label>
              <CategorySelect
                categories={filteredCategories}
                value={form.categoryId}
                onChange={categoryId => setForm(f => ({ ...f, categoryId, subcategoryId: null }))}
              />
            </div>

            {/* Subcategoria */}
            {subcategories.length > 0 && (
              <div>
                <label style={labelStyle}>Subcategoria</label>
                <select
                  value={form.subcategoryId ?? ''}
                  onChange={e => setForm(f => ({ ...f, subcategoryId: e.target.value || null }))}
                  style={inputStyle}
                  onFocus={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
                  onBlur={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
                >
                  <option value="">Nenhuma</option>
                  {subcategories.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
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
                  style={{ accentColor: 'var(--ff-emerald)', width: 16, height: 16 }} // Modificado
                />
                <span className="text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
                  Transação recorrente
                </span>
              </label>
              {form.isRecurring && (
                <select
                  value={form.recurrenceType}
                  onChange={e => setForm(f => ({ ...f, recurrenceType: Number(e.target.value) as RecurrenceType }))}
                  style={inputStyle}
                  onFocus={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
                  onBlur={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
                >
                  <option value={RecurrenceType.Daily}>Diária</option>
                  <option value={RecurrenceType.Weekly}>Semanal</option>
                  <option value={RecurrenceType.Monthly}>Mensal</option>
                  <option value={RecurrenceType.Yearly}>Anual</option>
                </select>
              )}
            </div>

            {/* Comprovante */}
            <div>
              <label style={labelStyle}>Comprovante</label>
              {attachment ? (
                <div
                  className="flex items-center gap-3 rounded-xl px-3 py-2.5"
                  style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
                >
                  <div
                    className="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
                    style={{ background: 'var(--ff-bg-card)' }}
                  >
                    {attachment.type.startsWith('image/')
                      ? <Image size={16} style={{ color: 'var(--ff-emerald)' }} />
                      : <FileText size={16} style={{ color: 'var(--ff-emerald)' }} />
                    }
                  </div>
                  <span className="text-sm truncate flex-1" style={{ color: 'var(--ff-text-secondary)' }}>
                    {attachment.name}
                  </span>
                  <button
                    onClick={() => setAttachment(null)}
                    className="flex-shrink-0 transition-colors"
                    style={{ color: 'var(--ff-text-muted)' }}
                    onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-expense)')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                  >
                    <X size={14} />
                  </button>
                </div>
              ) : currentAttachmentPath ? (
                <div
                  className="flex items-center gap-3 rounded-xl px-3 py-2.5"
                  style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
                >
                  <div
                    className="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
                    style={{ background: 'var(--ff-bg-card)' }}
                  >
                    <AttachmentViewer
                      transactionId={transaction!.id}
                      fileName={currentAttachmentName ?? currentAttachmentPath.split('/').pop() ?? 'comprovante'}
                      triggerIcon="file"
                    />
                  </div>
                  <span className="text-sm truncate flex-1" style={{ color: 'var(--ff-text-secondary)' }}>
                    {currentAttachmentName ?? currentAttachmentPath.split('/').pop()}
                  </span>
                  <div className="flex items-center gap-2 flex-shrink-0">
                    <button
                      onClick={() => attachmentInputRef.current?.click()}
                      className="text-xs transition-colors"
                      style={{ color: 'var(--ff-emerald)' }}
                      onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald-hover)')}
                      onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
                    >
                      Substituir
                    </button>
                    <span style={{ color: 'var(--ff-border)' }}>|</span>
                    <button
                      onClick={() => removeAttachment.mutate(undefined, {
                        onSuccess: () => { setCurrentAttachmentPath(null); setCurrentAttachmentName(null) }
                      })}
                      disabled={removeAttachment.isPending}
                      className="text-xs disabled:opacity-50 transition-colors"
                      style={{ color: 'var(--ff-expense)' }}
                    >
                      {removeAttachment.isPending ? 'Removendo...' : 'Remover'}
                    </button>
                  </div>
                </div>
              ) : (
                <button
                  onClick={() => attachmentInputRef.current?.click()}
                  className="w-full flex items-center justify-center gap-2 rounded-xl py-3 text-sm transition-colors"
                  style={{ border: '1px dashed var(--ff-border)', color: 'var(--ff-text-muted)' }}
                  onMouseEnter={e => {
                    e.currentTarget.style.color = 'var(--ff-text-secondary)'
                    e.currentTarget.style.borderColor = 'var(--ff-emerald)'
                  }}
                  onMouseLeave={e => {
                    e.currentTarget.style.color = 'var(--ff-text-muted)'
                    e.currentTarget.style.borderColor = 'var(--ff-border)'
                  }}
                >
                  <Paperclip size={14} />
                  Adicionar comprovante
                </button>
              )}
              <input
                ref={attachmentInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp,application/pdf"
                onChange={e => setAttachment(e.target.files?.[0] ?? null)}
                className="hidden"
              />
            </div>

            {/* Tags */}
            <div>
              <label style={labelStyle}>Tags</label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={tagInput}
                  onChange={e => setTagInput(e.target.value)}
                  onKeyDown={e => e.key === 'Enter' && (e.preventDefault(), addTag())}
                  placeholder="Adicionar tag..."
                  style={inputStyle}
                  onFocus={e => (e.target.style.borderColor = 'var(--ff-emerald)')}
                  onBlur={e => (e.target.style.borderColor = 'var(--ff-border)')}
                />
                <button
                  onClick={addTag}
                  className="px-4 py-2.5 rounded-xl text-sm transition-colors"
                  style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
                  onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
                  onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
                >
                  +
                </button>
              </div>
              {form.tags.length > 0 && (
                <div className="flex flex-wrap gap-1.5 mt-2">
                  {form.tags.map(tag => (
                    <span
                      key={tag}
                      className="flex items-center gap-1 text-xs px-2 py-1 rounded-lg"
                      style={{
                        background: 'var(--ff-bg-elevated)',
                        color:      'var(--ff-text-secondary)',
                        border:     '1px solid var(--ff-border)',
                      }}
                    >
                      {tag}
                      <button
                        onClick={() => removeTag(tag)}
                        className="transition-colors"
                        style={{ color: 'var(--ff-text-muted)' }}
                        onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-expense)')}
                        onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
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
          <div
            className="flex gap-3 px-6 py-4"
            style={{ borderTop: '1px solid var(--ff-border)' }}
          >
            <button
              onClick={onClose}
              className="flex-1 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors"
              style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
              onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
            >
              Cancelar
            </button>
            <button
              onClick={isEditing ? handleSubmit : handleCreate}
              disabled={isPending || !form.categoryId || form.amount <= 0}
              className="flex-1 px-4 py-2.5 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50"
              style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }} // Modificado
              onMouseEnter={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)' }}
              onMouseLeave={e => { if (!isPending) e.currentTarget.style.background = 'var(--ff-emerald)' }}
            >
              {isPending ? 'Salvando...' : isEditing ? 'Atualizar' : 'Criar'}
            </button>
          </div>
        </div>
      </div>

      {/* Modal de propagação */}
      {showPropagateModal && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
          <div
            className="rounded-2xl p-6 w-full max-w-md shadow-2xl"
            style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
          >
            <div
              className="w-12 h-12 rounded-xl flex items-center justify-center mb-4"
              style={{ background: 'var(--ff-emerald-subtle)', border: '1px solid rgba(16,185,129,0.2)' }}
            >
              <RefreshCw size={22} style={{ color: 'var(--ff-emerald)' }} />
            </div>

            <h2 className="font-semibold text-lg mb-1" style={{ color: 'var(--ff-text-primary)' }}>
              Editar transação recorrente
            </h2>
            <p className="text-sm mb-6" style={{ color: 'var(--ff-text-muted)' }}>
              Esta transação faz parte de um grupo recorrente. Como deseja aplicar as alterações?
            </p>

            <div className="flex flex-col gap-2">
              <button
                onClick={() => executarUpdate(false)}
                disabled={isPending}
                className="w-full px-4 py-3 rounded-xl text-sm font-medium transition-colors text-left disabled:opacity-50"
                style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-primary)' }}
                onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
                onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
              >
                <p className="font-semibold">Apenas esta</p>
                <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
                  Altera somente esta ocorrência. As futuras permanecem inalteradas.
                </p>
              </button>
              <button
                onClick={() => executarUpdate(true)}
                disabled={isPending}
                className="w-full px-4 py-3 rounded-xl text-sm font-medium transition-colors text-left disabled:opacity-50"
                style={{
                  border:     '1px solid rgba(16,185,129,0.3)',
                  background: 'rgba(16,185,129,0.05)',
                  color:      'var(--ff-text-primary)',
                }}
                onMouseEnter={e => (e.currentTarget.style.background = 'rgba(16,185,129,0.1)')}
                onMouseLeave={e => (e.currentTarget.style.background = 'rgba(16,185,129,0.05)')}
              >
                <p className="font-semibold" style={{ color: 'var(--ff-emerald)' }}>
                  Esta e todas as futuras
                </p>
                <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
                  Aplica as alterações a todas as ocorrências futuras do grupo.
                </p>
              </button>
              <button
                onClick={() => setShowPropagateModal(false)}
                className="w-full px-4 py-2.5 rounded-xl text-sm transition-colors mt-1"
                style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-muted)' }}
                onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
                onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
              >
                Cancelar
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}