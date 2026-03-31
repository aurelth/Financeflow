import { TrendingUp, TrendingDown, Minus } from 'lucide-react'
import { cn } from '@/lib/utils'
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
  if (value === null || value === undefined) {
    return <span className="text-slate-600 text-xs">—</span>
  }

  const isPositive = value > 0
  const isZero     = value === 0

  return (
    <span className={cn(
      'flex items-center justify-end gap-0.5 text-xs font-semibold',
      isZero     ? 'text-slate-400' :
      isPositive ? 'text-red-400' :
                   'text-emerald-400'
    )}>
      {isZero
        ? <Minus size={10} />
        : isPositive
          ? <TrendingUp size={10} />
          : <TrendingDown size={10} />
      }
      {isZero ? '0%' : `${Math.abs(value).toFixed(1)}%`}
    </span>
  )
}

export default function ComparisonTable({ categories, periods }: ComparisonTableProps) {
  if (categories.length === 0) {
    return (
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
        <div className="mb-4">
          <h3 className="text-slate-200 font-semibold text-sm">Detalhamento por Categoria</h3>
        </div>
        <div className="flex items-center justify-center py-8">
          <p className="text-slate-500 text-sm">Nenhuma despesa nos períodos selecionados</p>
        </div>
      </div>
    )
  }

  const periodLabels = periods.map(p => `${MONTHS[p.month - 1]}/${p.year}`)

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden">
      <div className="px-5 py-4 border-b border-slate-800">
        <h3 className="text-slate-200 font-semibold text-sm">Detalhamento por Categoria</h3>
        <p className="text-slate-500 text-xs mt-0.5">Delta absoluto e percentual entre períodos</p>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-800">
              <th className="text-left px-5 py-3 text-slate-400 font-medium">Categoria</th>
              {periodLabels.map((label, i) => (
                <th key={i} className="text-right px-4 py-3 text-slate-400 font-medium">
                  {label}
                </th>
              ))}
              {periods.length > 1 && (
                <th className="text-right px-4 py-3 text-slate-400 font-medium">Variação</th>
              )}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800">
            {categories.map((cat, index) => {
              const delta = cat.values.length > 1
                ? cat.values[cat.values.length - 1] - cat.values[0]
                : null

              return (
                <tr
                  key={index}
                  className="hover:bg-slate-800/50 transition-colors"
                >
                  <td className="px-5 py-3">
                    <div className="flex items-center gap-2">
                      <div
                        className="w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0"
                        style={{ backgroundColor: `${cat.categoryColor}20` }}
                      >
                        <CategoryIcon icon={cat.categoryIcon} color={cat.categoryColor} size={14} />
                      </div>
                      <span className="text-slate-300">{cat.categoryName}</span>
                    </div>
                  </td>

                  {cat.values.map((value, i) => (
                    <td key={i} className="px-4 py-3 text-right">
                      <div className="flex flex-col items-end gap-0.5">
                        <span className="text-slate-200 font-medium">
                          {value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                        </span>
                        {i > 0 && (
                          <VariationCell value={cat.variations[i]} />
                        )}
                      </div>
                    </td>
                  ))}

                  {periods.length > 1 && delta !== null && (
                    <td className="px-4 py-3 text-right">
                      <span className={cn(
                        'text-sm font-semibold',
                        delta > 0 ? 'text-red-400' : delta < 0 ? 'text-emerald-400' : 'text-slate-400'
                      )}>
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