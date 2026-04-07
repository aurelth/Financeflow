import { ArrowRight } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import CategoryIcon from '@/features/categories/components/CategoryIcon'
import type { BudgetSummary } from '@/features/budgets/types/budget.types'

interface TopBudgetsWidgetProps {
  summaries: BudgetSummary[]
}

// Usa tokens CSS da nova paleta
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

export default function TopBudgetsWidget({ summaries }: TopBudgetsWidgetProps) {
  const navigate = useNavigate()
  const top3     = [...summaries].sort((a, b) => b.percentage - a.percentage).slice(0, 3)

  return (
    <div
      className="rounded-2xl p-5"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
            Top Orçamentos
          </h3>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            3 categorias com maior uso
          </p>
        </div>
        <button
          onClick={() => navigate('/budgets')}
          className="flex items-center gap-1 text-xs transition-colors"
          style={{ color: 'var(--ff-emerald)' }}
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald-hover)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
        >
          Ver todos
          <ArrowRight size={12} />
        </button>
      </div>

      {top3.length === 0 ? (
        <div className="flex items-center justify-center py-8">
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Nenhum orçamento no período
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {top3.map(summary => (
            <div key={summary.id} className="space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div
                    className="w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0"
                    style={{
                      backgroundColor: `${summary.categoryColor}20`,
                      border:          `1px solid ${summary.categoryColor}30`,
                    }}
                  >
                    <CategoryIcon icon={summary.categoryIcon} color={summary.categoryColor} size={14} />
                  </div>
                  <span className="text-sm" style={{ color: 'var(--ff-text-secondary)' }}>
                    {summary.categoryName}
                  </span>
                </div>
                <span className="text-xs font-semibold" style={{ color: getPercentageColor(summary.percentage) }}>
                  {summary.percentage.toFixed(1)}%
                </span>
              </div>

              <div
                className="h-1.5 rounded-full overflow-hidden"
                style={{ background: 'var(--ff-bg-elevated)' }}
              >
                <div
                  className="h-full rounded-full transition-all duration-500"
                  style={{
                    width:      `${Math.min(summary.percentage, 100)}%`,
                    background: getProgressColor(summary.percentage),
                  }}
                />
              </div>

              <div className="flex justify-between text-xs" style={{ color: 'var(--ff-text-muted)' }}>
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