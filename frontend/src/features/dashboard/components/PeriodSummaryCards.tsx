import { TrendingUp, TrendingDown, Minus } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { PeriodData } from '../types/dashboard.types'

interface PeriodSummaryCardsProps {
  periods: PeriodData[]
}

const PERIOD_COLORS = [
  { bg: 'bg-indigo-500/10', border: 'border-indigo-500/20', text: 'text-indigo-400', label: 'bg-indigo-500' },
  { bg: 'bg-emerald-500/10', border: 'border-emerald-500/20', text: 'text-emerald-400', label: 'bg-emerald-500' },
  { bg: 'bg-amber-500/10', border: 'border-amber-500/20', text: 'text-amber-400', label: 'bg-amber-500' },
]

const MONTHS = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
]

function VariationBadge({ value }: { value: number | null }) {
  if (value === null) return null

  const isPositive = value > 0
  const isZero     = value === 0

  return (
    <span className={cn(
      'flex items-center gap-0.5 text-xs font-semibold px-1.5 py-0.5 rounded-lg',
      isZero     ? 'bg-slate-700 text-slate-400' :
      isPositive ? 'bg-red-500/10 text-red-400' :
                   'bg-emerald-500/10 text-emerald-400'
    )}>
      {isZero ? <Minus size={10} /> : isPositive ? <TrendingUp size={10} /> : <TrendingDown size={10} />}
      {isZero ? '0%' : `${Math.abs(value).toFixed(1)}%`}
    </span>
  )
}

function calcVariation(current: number, previous: number): number | null {
  if (previous === 0) return null
  return Math.round((current - previous) / previous * 1000) / 10
}

export default function PeriodSummaryCards({ periods }: PeriodSummaryCardsProps) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
      {periods.map((period, index) => {
        const prev          = periods[index - 1]
        const incomeVar     = prev ? calcVariation(period.totalIncome, prev.totalIncome) : null
        const expensesVar   = prev ? calcVariation(period.totalExpenses, prev.totalExpenses) : null
        const balanceVar    = prev ? calcVariation(period.balance, prev.balance) : null
        const colors        = PERIOD_COLORS[index]

        return (
          <div
            key={index}
            className={cn('rounded-2xl border p-5 space-y-4', colors.bg, colors.border)}
          >
            {/* Header */}
            <div className="flex items-center gap-2">
              <span className={cn('w-2.5 h-2.5 rounded-full', colors.label)} />
              <span className={cn('text-sm font-semibold', colors.text)}>
                {MONTHS[period.month - 1]} {period.year}
              </span>
            </div>

            {/* Valores */}
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-400">Receitas</span>
                <div className="flex items-center gap-2">
                  <VariationBadge value={incomeVar} />
                  <span className="text-sm font-semibold text-emerald-400">
                    {period.totalIncome.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </span>
                </div>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-400">Despesas</span>
                <div className="flex items-center gap-2">
                  <VariationBadge value={expensesVar} />
                  <span className="text-sm font-semibold text-red-400">
                    {period.totalExpenses.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </span>
                </div>
              </div>

              <div className="h-px bg-slate-700" />

              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-400">Saldo</span>
                <div className="flex items-center gap-2">
                  <VariationBadge value={balanceVar} />
                  <span className={cn(
                    'text-sm font-bold',
                    period.balance >= 0 ? 'text-slate-200' : 'text-red-400'
                  )}>
                    {period.balance.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </span>
                </div>
              </div>
            </div>
          </div>
        )
      })}
    </div>
  )
}