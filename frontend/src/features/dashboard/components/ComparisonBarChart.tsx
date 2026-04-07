import {
  BarChart, Bar, XAxis, YAxis,
  CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import type { CategoryComparison, PeriodData } from '../types/dashboard.types'

interface ComparisonBarChartProps {
  categories: CategoryComparison[]
  periods:    PeriodData[]
}

const MONTHS = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
]

const PERIOD_COLORS = ['#6366f1', '#10b981', '#f59e0b']

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function ComparisonBarChart({ categories, periods }: ComparisonBarChartProps) {
  if (categories.length === 0) {
    return (
      <div
        className="rounded-2xl p-5"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="mb-4">
          <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
            Despesas por Categoria
          </h3>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Comparativo entre períodos
          </p>
        </div>
        <div className="flex items-center justify-center h-[220px]">
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Nenhuma despesa nos períodos selecionados
          </p>
        </div>
      </div>
    )
  }

  const top8 = [...categories]
    .sort((a, b) => Math.max(...b.values) - Math.max(...a.values))
    .slice(0, 8)

  const chartData = top8.map(cat => {
    const entry: Record<string, any> = { name: cat.categoryName }
    periods.forEach((p, i) => {
      entry[`${MONTHS[p.month - 1]}/${p.year}`] = cat.values[i] ?? 0
    })
    return entry
  })

  const periodLabels = periods.map(p => `${MONTHS[p.month - 1]}/${p.year}`)

  return (
    <div
      className="rounded-2xl p-5"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="mb-4">
        <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
          Despesas por Categoria
        </h3>
        <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          Comparativo entre períodos
        </p>
      </div>

      <ResponsiveContainer width="100%" height={280}>
        <BarChart data={chartData} margin={{ top: 5, right: 10, left: 10, bottom: 5 }} barGap={2}>
          <CartesianGrid strokeDasharray="3 3" stroke="#1a1a1a" vertical={false} />
          <XAxis
            dataKey="name"
            tick={{ fill: '#52525b', fontSize: 11 }}
            axisLine={{ stroke: '#222222' }}
            tickLine={false}
          />
          <YAxis
            tickFormatter={v => `R$ ${(v / 1000).toFixed(0)}k`}
            tick={{ fill: '#52525b', fontSize: 11 }}
            axisLine={false}
            tickLine={false}
            width={60}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: '#111111',
              border: '1px solid #222222',
              borderRadius: '12px',
              fontSize: '12px',
              color: '#a1a1aa',
            }}
            formatter={(value: any) => [formatCurrency(Number(value))]}
          />
          <Legend wrapperStyle={{ fontSize: 12, color: '#52525b' }} />
          {periodLabels.map((label, i) => (
            <Bar key={label} dataKey={label} fill={PERIOD_COLORS[i]} radius={[4, 4, 0, 0]} maxBarSize={28} />
          ))}
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}