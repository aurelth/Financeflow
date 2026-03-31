import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
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
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
        <div className="mb-4">
          <h3 className="text-slate-200 font-semibold text-sm">Despesas por Categoria</h3>
          <p className="text-slate-500 text-xs mt-0.5">Comparativo entre períodos</p>
        </div>
        <div className="flex items-center justify-center h-[220px]">
          <p className="text-slate-500 text-sm">Nenhuma despesa nos períodos selecionados</p>
        </div>
      </div>
    )
  }

  // Monta dados para o gráfico — top 8 categorias por valor máximo
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
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
      <div className="mb-4">
        <h3 className="text-slate-200 font-semibold text-sm">Despesas por Categoria</h3>
        <p className="text-slate-500 text-xs mt-0.5">Comparativo entre períodos</p>
      </div>

      <ResponsiveContainer width="100%" height={280}>
        <BarChart data={chartData} margin={{ top: 5, right: 10, left: 10, bottom: 5 }} barGap={2}>
          <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
          <XAxis
            dataKey="name"
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
            <Bar
              key={label}
              dataKey={label}
              fill={PERIOD_COLORS[i]}
              radius={[4, 4, 0, 0]}
              maxBarSize={28}
            />
          ))}
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}