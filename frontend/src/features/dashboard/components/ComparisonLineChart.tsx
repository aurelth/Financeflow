import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
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
  // Monta dados para comparação de receitas, despesas e saldo entre períodos
  const chartData = [
  {
    metric: 'Receitas',
    ...Object.fromEntries(periods.map(p => [
      `${MONTHS[p.month - 1]}/${p.year}`, p.totalIncome
    ])),
  },
  {
    metric: 'Despesas',
    ...Object.fromEntries(periods.map(p => [
      `${MONTHS[p.month - 1]}/${p.year}`, p.totalExpenses
    ])),
  },
  {
    metric: 'Saldo',
    ...Object.fromEntries(periods.map(p => [
      `${MONTHS[p.month - 1]}/${p.year}`, p.balance
    ])),
  },
]

  const periodLabels = periods.map(p => `${MONTHS[p.month - 1]}/${p.year}`)

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
      <div className="mb-4">
        <h3 className="text-slate-200 font-semibold text-sm">Evolução entre Períodos</h3>
        <p className="text-slate-500 text-xs mt-0.5">Receitas, despesas e saldo por período</p>
      </div>

      <ResponsiveContainer width="100%" height={220}>
        <LineChart data={chartData} margin={{ top: 5, right: 10, left: 10, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
          <XAxis
            dataKey="metric"
            tick={{ fill: '#64748b', fontSize: 11 }}
            axisLine={{ stroke: '#1e293b' }}
            tickLine={false}
          />
          <YAxis
            tickFormatter={v => `R$ ${(v / 1000).toFixed(0)}k`}
            tick={{ fill: '#64748b', fontSize: 11 }}
            axisLine={false}
            tickLine={false}
            width={60}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: '#0f172a',
              border: '1px solid #1e293b',
              borderRadius: '12px',
              fontSize: '12px',
            }}
            formatter={(value: any) => [formatCurrency(Number(value))]}
          />
          <Legend wrapperStyle={{ fontSize: 12, color: '#94a3b8' }} />
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