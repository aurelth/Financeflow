import { TrendingUp, TrendingDown, Minus } from 'lucide-react'
import type { PeriodData } from '../types/dashboard.types'

interface PeriodSummaryCardsProps {
  periods: PeriodData[]
}

// Usa tokens CSS da nova paleta
const PERIOD_STYLES = [
  {
    bg:     'rgba(99, 102, 241, 0.08)',
    border: 'rgba(99, 102, 241, 0.2)',
    dot:    '#6366f1',
    text:   '#6366f1',
  },
  {
    bg:     'rgba(16, 185, 129, 0.08)',
    border: 'rgba(16, 185, 129, 0.2)',
    dot:    '#10b981',
    text:   '#10b981',
  },
  {
    bg:     'rgba(245, 158, 11, 0.08)',
    border: 'rgba(245, 158, 11, 0.2)',
    dot:    '#f59e0b',
    text:   '#f59e0b',
  },
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
    <span
      className="flex items-center gap-0.5 text-xs font-semibold px-1.5 py-0.5 rounded-lg"
      style={{
        background: isZero ? 'var(--ff-bg-elevated)' : isPositive ? 'rgba(244,63,94,0.1)' : 'rgba(16,185,129,0.1)',
        color:      isZero ? 'var(--ff-text-muted)'  : isPositive ? 'var(--ff-expense)'    : 'var(--ff-income)',
      }}
    >
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
        const prev        = periods[index - 1]
        const incomeVar   = prev ? calcVariation(period.totalIncome, prev.totalIncome) : null
        const expensesVar = prev ? calcVariation(period.totalExpenses, prev.totalExpenses) : null
        const balanceVar  = prev ? calcVariation(period.balance, prev.balance) : null
        const s           = PERIOD_STYLES[index]

        return (
          <div
            key={index}
            className="rounded-2xl p-5 space-y-4"
            style={{ background: s.bg, border: `1px solid ${s.border}` }}
          >
            <div className="flex items-center gap-2">
              <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ background: s.dot }} />
              <span className="text-sm font-semibold" style={{ color: s.text }}>
                {MONTHS[period.month - 1]} {period.year}
              </span>
            </div>

            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>Receitas</span>
                <div className="flex items-center gap-2">
                  <VariationBadge value={incomeVar} />
                  <span className="text-sm font-semibold" style={{ color: 'var(--ff-income)' }}>
                    {period.totalIncome.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </span>
                </div>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>Despesas</span>
                <div className="flex items-center gap-2">
                  <VariationBadge value={expensesVar} />
                  <span className="text-sm font-semibold" style={{ color: 'var(--ff-expense)' }}>
                    {period.totalExpenses.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </span>
                </div>
              </div>

              <div className="h-px" style={{ background: 'var(--ff-border)' }} />

              <div className="flex items-center justify-between">
                <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>Saldo</span>
                <div className="flex items-center gap-2">
                  <VariationBadge value={balanceVar} />
                  <span
                    className="text-sm font-bold"
                    style={{ color: period.balance >= 0 ? 'var(--ff-text-primary)' : 'var(--ff-expense)' }}
                  >
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