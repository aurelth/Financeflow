import {
  LineChart, Line, XAxis, YAxis,
  CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import type { PeriodData } from '../types/dashboard.types'

interface ComparisonLineChartProps {
  periods: PeriodData[]
}

const MONTHS = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
]

const PERIOD_COLORS = ['#6366f1', '#10b981', '#f59e0b']

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function ComparisonLineChart({ periods }: ComparisonLineChartProps) {
  const chartData = [
    {
      metric: 'Receitas',
      ...Object.fromEntries(periods.map(p => [`${MONTHS[p.month - 1]}/${p.year}`, p.totalIncome])),
    },
    {
      metric: 'Despesas',
      ...Object.fromEntries(periods.map(p => [`${MONTHS[p.month - 1]}/${p.year}`, p.totalExpenses])),
    },
    {
      metric: 'Saldo',
      ...Object.fromEntries(periods.map(p => [`${MONTHS[p.month - 1]}/${p.year}`, p.balance])),
    },
  ]

  const periodLabels = periods.map(p => `${MONTHS[p.month - 1]}/${p.year}`)

  return (
    <div
      className="rounded-2xl p-5"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="mb-4">
        <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
          Evolução entre Períodos
        </h3>
        <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          Receitas, despesas e saldo por período
        </p>
      </div>

      <ResponsiveContainer width="100%" height={220}>
        <LineChart data={chartData} margin={{ top: 5, right: 10, left: 10, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#1a1a1a" />
          <XAxis
            dataKey="metric"
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
            <Line
              key={label}
              type="monotone"
              dataKey={label}
              stroke={PERIOD_COLORS[i]}
              strokeWidth={2}
              dot={{ fill: PERIOD_COLORS[i], r: 5 }}
              activeDot={{ r: 6 }}
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}