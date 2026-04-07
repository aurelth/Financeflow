import {
  BarChart, Bar, XAxis, YAxis,
  CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import type { WeeklyComparison } from '../types/dashboard.types'

interface BarChartCardProps {
  data: WeeklyComparison[]
}

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function BarChartCard({ data }: BarChartCardProps) {
  return (
    <div
      className="rounded-2xl p-5"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="mb-4">
        <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
          Comparação Semanal
        </h3>
        <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          Receitas vs despesas por semana
        </p>
      </div>

      <ResponsiveContainer width="100%" height={220}>
        <BarChart data={data} margin={{ top: 5, right: 10, left: 10, bottom: 5 }} barGap={4}>
          <CartesianGrid strokeDasharray="3 3" stroke="#1a1a1a" vertical={false} />
          <XAxis
            dataKey="label"
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
            formatter={(value: any, name: any) => [
              formatCurrency(Number(value)),
              name === 'income' ? 'Receita' : 'Despesa',
            ]}
          />
          <Legend
            formatter={(value: any) => value === 'income' ? 'Receita' : 'Despesa'}
            wrapperStyle={{ fontSize: 12, color: '#52525b' }}
          />
          <Bar dataKey="income"   fill="#10b981" radius={[4, 4, 0, 0]} maxBarSize={32} />
          <Bar dataKey="expenses" fill="#f43f5e" radius={[4, 4, 0, 0]} maxBarSize={32} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}