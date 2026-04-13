import { useState } from 'react'
import { ArrowDownCircle, ArrowUpCircle, AlertCircle } from 'lucide-react'
import type { BankImportTransactionDto } from '../types/imports.types'
import type { Category } from '@/features/categories/types/category.types'

interface Props {
  transactions: BankImportTransactionDto[]
  categories: Category[]
  onChange: (updated: BankImportTransactionDto[]) => void
}

function formatAmount(amount: number, type: 'Income' | 'Expense') {
  const formatted = new Intl.NumberFormat('pt-PT', {
    style: 'currency', currency: 'BRL',
  }).format(amount)
  return type === 'Expense' ? `- ${formatted}` : `+ ${formatted}`
}

export default function ImportPreviewTable({ transactions, categories, onChange }: Props) {
  const [selectAll, setSelectAll] = useState(true)

  function toggleAll(checked: boolean) {
    setSelectAll(checked)
    onChange(transactions.map(t => ({ ...t, isSelected: t.isDuplicate ? false : checked })))
  }

  function toggleOne(id: string, checked: boolean) {
    onChange(transactions.map(t => t.id === id ? { ...t, isSelected: checked } : t))
  }

  function changeCategory(id: string, categoryId: string) {
    onChange(transactions.map(t =>
      t.id === id ? { ...t, suggestedCategoryId: categoryId || null } : t
    ))
  }

  const selectedCount = transactions.filter(t => t.isSelected).length

  return (
    <div className="space-y-3">

      {/* Resumo */}
      <div className="flex items-center justify-between px-1">
        <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
          <span style={{ color: 'var(--ff-text-primary)', fontWeight: 500 }}>{selectedCount}</span>
          {' '}de{' '}
          <span style={{ color: 'var(--ff-text-primary)', fontWeight: 500 }}>{transactions.length}</span>
          {' '}transações selecionadas
        </p>
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            checked={selectAll}
            onChange={e => toggleAll(e.target.checked)}
            className="rounded"
            style={{ accentColor: 'var(--ff-emerald)' }}
          />
          <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
            Selecionar todas
          </span>
        </label>
      </div>

      {/* Tabela */}
      <div
        className="rounded-xl overflow-hidden"
        style={{ border: '1px solid var(--ff-border)' }}
      >
        {/* Header */}
        <div
          className="grid gap-4 px-4 py-2.5 text-xs font-medium"
          style={{
            gridTemplateColumns: '32px 1fr 120px 140px 100px',
            background: 'var(--ff-bg-elevated)',
            color: 'var(--ff-text-muted)',
          }}
        >
          <span />
          <span>Descrição</span>
          <span>Data</span>
          <span>Categoria</span>
          <span className="text-right">Valor</span>
        </div>

        {/* Rows */}
        <div className="divide-y" style={{ borderColor: 'var(--ff-border)' }}>
          {transactions.map(t => {
            const date = new Date(`${t.date}Z`).toLocaleDateString('pt-PT', {
              day: '2-digit', month: '2-digit', year: 'numeric',
            })

            return (
              <div
                key={t.id}
                className="grid gap-4 px-4 py-3 items-center transition-colors"
                style={{
                  gridTemplateColumns: '32px 1fr 120px 140px 100px',
                  background: t.isDuplicate
                    ? 'rgba(248,113,113,0.04)'
                    : t.isSelected
                      ? 'var(--ff-bg-card)'
                      : 'var(--ff-bg-elevated)',
                  opacity: t.isDuplicate ? 0.6 : 1,
                }}
              >
                {/* Checkbox */}
                <input
                  type="checkbox"
                  checked={t.isSelected}
                  disabled={t.isDuplicate}
                  onChange={e => toggleOne(t.id, e.target.checked)}
                  className="rounded"
                  style={{ accentColor: 'var(--ff-emerald)' }}
                />

                {/* Descrição */}
                <div className="flex items-center gap-2 min-w-0">
                  {t.type === 'Expense'
                    ? <ArrowDownCircle size={15} style={{ color: '#f87171', flexShrink: 0 }} />
                    : <ArrowUpCircle  size={15} style={{ color: '#34d399', flexShrink: 0 }} />
                  }
                  <span
                    className="text-sm truncate"
                    style={{ color: 'var(--ff-text-primary)' }}
                    title={t.description}
                  >
                    {t.description}
                  </span>
                  {t.isDuplicate && (
                    <span
                      className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-xs flex-shrink-0"
                      style={{ background: 'rgba(248,113,113,0.1)', color: '#f87171' }}
                    >
                      <AlertCircle size={10} />
                      duplicada
                    </span>
                  )}
                </div>

                {/* Data */}
                <span className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
                  {date}
                </span>

                {/* Categoria */}
                <select
                  value={t.suggestedCategoryId ?? ''}
                  disabled={t.isDuplicate}
                  onChange={e => changeCategory(t.id, e.target.value)}
                  className="text-xs rounded-lg px-2 py-1.5 outline-none w-full"
                  style={{
                    background: 'var(--ff-bg-elevated)',
                    color: t.suggestedCategoryId ? 'var(--ff-text-primary)' : 'var(--ff-text-muted)',
                    border: '1px solid var(--ff-border)',
                  }}
                >
                  <option value="">Sem categoria</option>
                  {categories
                    .filter(c => c.type === (t.type === 'Expense' ? 2 : 1))
                    .map(c => (
                      <option key={c.id} value={c.id}>{c.name}</option>
                    ))
                  }
                </select>

                {/* Valor */}
                <span
                  className="text-sm font-medium text-right"
                  style={{ color: t.type === 'Expense' ? '#f87171' : '#34d399' }}
                >
                  {formatAmount(t.amount, t.type)}
                </span>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}