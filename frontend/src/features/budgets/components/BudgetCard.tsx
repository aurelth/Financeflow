import { Pencil, Trash2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import CategoryIcon from '../../categories/components/CategoryIcon'
import type { BudgetSummary } from '../types/budget.types'

interface BudgetCardProps {
  summary:  BudgetSummary
  onEdit:   () => void
  onDelete: () => void
}

function getProgressColor(percentage: number): string {
  if (percentage >= 100) return 'bg-red-500'
  if (percentage >= 80)  return 'bg-amber-400'
  return 'bg-emerald-500'
}

function getPercentageColor(percentage: number): string {
  if (percentage >= 100) return 'text-red-400'
  if (percentage >= 80)  return 'text-amber-400'
  return 'text-emerald-400'
}

export default function BudgetCard({ summary, onEdit, onDelete }: BudgetCardProps) {
  const clampedPct     = Math.min(summary.percentage, 100)
  const progressColor  = getProgressColor(summary.percentage)
  const percentageColor = getPercentageColor(summary.percentage)
  const remaining      = Math.max(summary.limitAmount - summary.spentAmount, 0)

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 flex flex-col gap-4 hover:border-slate-700 transition-colors">

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
            <p className="text-slate-200 font-medium text-sm">{summary.categoryName}</p>
            <p className={cn('text-xs font-semibold', percentageColor)}>
              {summary.percentage.toFixed(1)}% utilizado
            </p>
          </div>
        </div>

        {/* Ações */}
        <div className="flex items-center gap-1">
          <button
            onClick={onEdit}
            className="p-1.5 text-slate-500 hover:text-indigo-400 hover:bg-slate-800 rounded-lg transition-all"
          >
            <Pencil size={14} />
          </button>
          <button
            onClick={onDelete}
            className="p-1.5 text-slate-500 hover:text-red-400 hover:bg-slate-800 rounded-lg transition-all"
          >
            <Trash2 size={14} />
          </button>
        </div>
      </div>

      {/* Barra de progresso */}
      <div>
        <div className="h-2 bg-slate-800 rounded-full overflow-hidden">
          <div
            className={cn('h-full rounded-full transition-all duration-500', progressColor)}
            style={{ width: `${clampedPct}%` }}
          />
        </div>
      </div>

      {/* Valores */}
      <div className="grid grid-cols-3 gap-2">
        <div className="flex flex-col">
          <span className="text-xs text-slate-500 mb-0.5">Gasto</span>
          <span className="text-sm font-semibold text-slate-200">
            {summary.spentAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </span>
        </div>
        <div className="flex flex-col items-center">
          <span className="text-xs text-slate-500 mb-0.5">Limite</span>
          <span className="text-sm font-semibold text-slate-200">
            {summary.limitAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </span>
        </div>
        <div className="flex flex-col items-end">
          <span className="text-xs text-slate-500 mb-0.5">Restante</span>
          <span className={cn('text-sm font-semibold', remaining === 0 ? 'text-red-400' : 'text-emerald-400')}>
            {remaining.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </span>
        </div>
      </div>

      {/* Alerta */}
      {summary.percentage >= 100 && (
        <div className="flex items-center gap-2 bg-red-500/10 border border-red-500/20 rounded-xl px-3 py-2">
          <span className="text-xs text-red-400 font-medium">⚠️ Limite atingido!</span>
        </div>
      )}
      {summary.percentage >= 80 && summary.percentage < 100 && (
        <div className="flex items-center gap-2 bg-amber-500/10 border border-amber-500/20 rounded-xl px-3 py-2">
          <span className="text-xs text-amber-400 font-medium">🔔 Atenção: 80% do limite atingido</span>
        </div>
      )}
    </div>
  )
}