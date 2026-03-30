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
import type { WeeklyComparison } from '../types/dashboard.types'

interface BarChartCardProps {
  data: WeeklyComparison[]
}

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function BarChartCard({ data }: BarChartCardProps) {
  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
      <div className="mb-4">
        <h3 className="text-slate-200 font-semibold text-sm">Comparação Semanal</h3>
        <p className="text-slate-500 text-xs mt-0.5">Receitas vs despesas por semana</p>
      </div>

      <ResponsiveContainer width="100%" height={220}>
        <BarChart data={data} margin={{ top: 5, right: 10, left: 10, bottom: 5 }} barGap={4}>
          <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
          <XAxis
            dataKey="label"
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
            formatter={(value: any, name: any) => [
              formatCurrency(Number(value)),
              name === 'income' ? 'Receita' : 'Despesa',
            ]}
          />
          <Legend
            formatter={(value: any) => value === 'income' ? 'Receita' : 'Despesa'}
            wrapperStyle={{ fontSize: 12, color: '#94a3b8' }}
          />
          <Bar dataKey="income"   fill="#10b981" radius={[4, 4, 0, 0]} maxBarSize={32} />
          <Bar dataKey="expenses" fill="#ef4444" radius={[4, 4, 0, 0]} maxBarSize={32} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}