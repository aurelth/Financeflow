import { TrendingUp, TrendingDown, Minus } from 'lucide-react'
import CategoryIcon from '@/features/categories/components/CategoryIcon'
import type { CategoryComparison, PeriodData } from '../types/dashboard.types'

interface ComparisonTableProps {
  categories: CategoryComparison[]
  periods:    PeriodData[]
}

const MONTHS = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
]

function VariationCell({ value }: { value: number | null | undefined }) {
  if (value === null || value === undefined)
    return <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>—</span>

  const isPositive = value > 0
  const isZero     = value === 0

  return (
    <span
      className="flex items-center justify-end gap-0.5 text-xs font-semibold"
      style={{
        color: isZero ? 'var(--ff-text-secondary)' : isPositive ? 'var(--ff-expense)' : 'var(--ff-income)',
      }}
    >
      {isZero ? <Minus size={10} /> : isPositive ? <TrendingUp size={10} /> : <TrendingDown size={10} />}
      {isZero ? '0%' : `${Math.abs(value).toFixed(1)}%`}
    </span>
  )
}

export default function ComparisonTable({ categories, periods }: ComparisonTableProps) {
  if (categories.length === 0) {
    return (
      <div
        className="rounded-2xl p-5"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="mb-4">
          <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
            Detalhamento por Categoria
          </h3>
        </div>
        <div className="flex items-center justify-center py-8">
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Nenhuma despesa nos períodos selecionados
          </p>
        </div>
      </div>
    )
  }

  const periodLabels = periods.map(p => `${MONTHS[p.month - 1]}/${p.year}`)

  return (
    <div
      className="rounded-2xl overflow-hidden"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="px-5 py-4" style={{ borderBottom: '1px solid var(--ff-border)' }}>
        <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
          Detalhamento por Categoria
        </h3>
        <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          Delta absoluto e percentual entre períodos
        </p>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr style={{ borderBottom: '1px solid var(--ff-border)' }}>
              <th className="text-left px-5 py-3 font-medium" style={{ color: 'var(--ff-text-muted)' }}>
                Categoria
              </th>
              {periodLabels.map((label, i) => (
                <th key={i} className="text-right px-4 py-3 font-medium" style={{ color: 'var(--ff-text-muted)' }}>
                  {label}
                </th>
              ))}
              {periods.length > 1 && (
                <th className="text-right px-4 py-3 font-medium" style={{ color: 'var(--ff-text-muted)' }}>
                  Variação
                </th>
              )}
            </tr>
          </thead>
          <tbody>
            {categories.map((cat, index) => {
              const delta = cat.values.length > 1
                ? cat.values[cat.values.length - 1] - cat.values[0]
                : null

              return (
                <tr
                  key={index}
                  className="transition-colors"
                  style={{ borderBottom: '1px solid var(--ff-border-subtle)' }}
                  onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
                  onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
                >
                  <td className="px-5 py-3">
                    <div className="flex items-center gap-2">
                      <div
                        className="w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0"
                        style={{ backgroundColor: `${cat.categoryColor}20` }}
                      >
                        <CategoryIcon icon={cat.categoryIcon} color={cat.categoryColor} size={14} />
                      </div>
                      <span style={{ color: 'var(--ff-text-secondary)' }}>{cat.categoryName}</span>
                    </div>
                  </td>

                  {cat.values.map((value, i) => (
                    <td key={i} className="px-4 py-3 text-right">
                      <div className="flex flex-col items-end gap-0.5">
                        <span className="font-medium" style={{ color: 'var(--ff-text-primary)' }}>
                          {value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                        </span>
                        {i > 0 && <VariationCell value={cat.variations[i]} />}
                      </div>
                    </td>
                  ))}

                  {periods.length > 1 && delta !== null && (
                    <td className="px-4 py-3 text-right">
                      <span
                        className="text-sm font-semibold"
                        style={{ color: delta > 0 ? 'var(--ff-expense)' : delta < 0 ? 'var(--ff-income)' : 'var(--ff-text-muted)' }}
                      >
                        {delta > 0 ? '+' : ''}
                        {delta.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                      </span>
                    </td>
                  )}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}