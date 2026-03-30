import { ArrowRight } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { cn } from '@/lib/utils'
import CategoryIcon from '@/features/categories/components/CategoryIcon'
import type { BudgetSummary } from '@/features/budgets/types/budget.types'

interface TopBudgetsWidgetProps {
  summaries: BudgetSummary[]
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

export default function TopBudgetsWidget({ summaries }: TopBudgetsWidgetProps) {
  const navigate  = useNavigate()
  const top3      = [...summaries].sort((a, b) => b.percentage - a.percentage).slice(0, 3)

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-slate-200 font-semibold text-sm">Top Orçamentos</h3>
          <p className="text-slate-500 text-xs mt-0.5">3 categorias com maior uso</p>
        </div>
        <button
          onClick={() => navigate('/budgets')}
          className="flex items-center gap-1 text-xs text-indigo-400 hover:text-indigo-300 transition-colors"
        >
          Ver todos
          <ArrowRight size={12} />
        </button>
      </div>

      {top3.length === 0 ? (
        <div className="flex items-center justify-center py-8">
          <p className="text-slate-500 text-sm">Nenhum orçamento no período</p>
        </div>
      ) : (
        <div className="space-y-4">
          {top3.map(summary => (
            <div key={summary.id} className="space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div
                    className="w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0"
                    style={{ backgroundColor: `${summary.categoryColor}20`, border: `1px solid ${summary.categoryColor}30` }}
                  >
                    <CategoryIcon icon={summary.categoryIcon} color={summary.categoryColor} size={14} />
                  </div>
                  <span className="text-slate-300 text-sm">{summary.categoryName}</span>
                </div>
                <span className={cn('text-xs font-semibold', getPercentageColor(summary.percentage))}>
                  {summary.percentage.toFixed(1)}%
                </span>
              </div>

              <div className="h-1.5 bg-slate-800 rounded-full overflow-hidden">
                <div
                  className={cn('h-full rounded-full transition-all duration-500', getProgressColor(summary.percentage))}
                  style={{ width: `${Math.min(summary.percentage, 100)}%` }}
                />
              </div>

              <div className="flex justify-between text-xs text-slate-500">
                <span>
                  {summary.spentAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                </span>
                <span>
                  {summary.limitAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}