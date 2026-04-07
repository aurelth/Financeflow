import { Pencil, Trash2 } from 'lucide-react'
import CategoryIcon from '../../categories/components/CategoryIcon'
import type { BudgetSummary } from '../types/budget.types'

interface BudgetCardProps {
  summary:  BudgetSummary
  onEdit:   () => void
  onDelete: () => void
}

function getProgressColor(percentage: number): string {
  if (percentage >= 100) return 'var(--ff-expense)'
  if (percentage >= 80)  return 'var(--ff-pending)'
  return 'var(--ff-income)'
}

function getPercentageColor(percentage: number): string {
  if (percentage >= 100) return 'var(--ff-expense)'
  if (percentage >= 80)  return 'var(--ff-pending)'
  return 'var(--ff-income)'
}

export default function BudgetCard({ summary, onEdit, onDelete }: BudgetCardProps) {
  const clampedPct  = Math.min(summary.percentage, 100)
  const remaining   = Math.max(summary.limitAmount - summary.spentAmount, 0)

  return (
    <div
      className="rounded-2xl p-5 flex flex-col gap-4 transition-colors"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      onMouseEnter={e => (e.currentTarget.style.borderColor = '#333333')}
      onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
    >
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div
            className="w-10 h-10 rounded-xl flex items-center justify-center text-lg flex-shrink-0"
            style={{ backgroundColor: `${summary.categoryColor}20`, border: `1px solid ${summary.categoryColor}40` }}
          >
            <CategoryIcon icon={summary.categoryIcon} color={summary.categoryColor} size={20} />
          </div>
          <div>
            <p className="font-medium text-sm" style={{ color: 'var(--ff-text-primary)' }}>
              {summary.categoryName}
            </p>
            <p className="text-xs font-semibold" style={{ color: getPercentageColor(summary.percentage) }}>
              {summary.percentage.toFixed(1)}% utilizado
            </p>
          </div>
        </div>

        <div className="flex items-center gap-1">
          <button
            onClick={onEdit}
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
            <Pencil size={14} />
          </button>
          <button
            onClick={onDelete}
            className="p-1.5 rounded-lg transition-all"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => {
              e.currentTarget.style.color = 'var(--ff-expense)'
              e.currentTarget.style.background = 'var(--ff-bg-elevated)'
            }}
            onMouseLeave={e => {
              e.currentTarget.style.color = 'var(--ff-text-muted)'
              e.currentTarget.style.background = 'transparent'
            }}
          >
            <Trash2 size={14} />
          </button>
        </div>
      </div>

      {/* Barra de progresso */}
      <div
        className="h-2 rounded-full overflow-hidden"
        style={{ background: 'var(--ff-bg-elevated)' }}
      >
        <div
          className="h-full rounded-full transition-all duration-500"
          style={{
            width:      `${clampedPct}%`,
            background: getProgressColor(summary.percentage),
          }}
        />
      </div>

      {/* Valores */}
      <div className="grid grid-cols-3 gap-2">
        <div className="flex flex-col">
          <span className="text-xs mb-0.5" style={{ color: 'var(--ff-text-muted)' }}>Gasto</span>
          <span className="text-sm font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            {summary.spentAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </span>
        </div>
        <div className="flex flex-col items-center">
          <span className="text-xs mb-0.5" style={{ color: 'var(--ff-text-muted)' }}>Limite</span>
          <span className="text-sm font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            {summary.limitAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </span>
        </div>
        <div className="flex flex-col items-end">
          <span className="text-xs mb-0.5" style={{ color: 'var(--ff-text-muted)' }}>Restante</span>
          <span
            className="text-sm font-semibold"
            style={{ color: remaining === 0 ? 'var(--ff-expense)' : 'var(--ff-income)' }}
          >
            {remaining.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </span>
        </div>
      </div>

      {/* Alertas */}
      {summary.percentage >= 100 && (
        <div
          className="flex items-center gap-2 rounded-xl px-3 py-2"
          style={{ background: 'rgba(244,63,94,0.08)', border: '1px solid rgba(244,63,94,0.2)' }}
        >
          <span className="text-xs font-medium" style={{ color: 'var(--ff-expense)' }}>
            ⚠️ Limite atingido!
          </span>
        </div>
      )}
      {summary.percentage >= 80 && summary.percentage < 100 && (
        <div
          className="flex items-center gap-2 rounded-xl px-3 py-2"
          style={{ background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.2)' }}
        >
          <span className="text-xs font-medium" style={{ color: 'var(--ff-pending)' }}>
            🔔 Atenção: 80% do limite atingido
          </span>
        </div>
      )}
    </div>
  )
}