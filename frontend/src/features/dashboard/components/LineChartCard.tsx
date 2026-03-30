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
import type { BalanceEvolution } from '../types/dashboard.types'

interface LineChartCardProps {
  data: BalanceEvolution[]
}

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

const formatDate = (date: string) => {
  const [, , day] = date.split('-')
  return `Dia ${parseInt(day)}`
}

export default function LineChartCard({ data }: LineChartCardProps) {
  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
      <div className="mb-4">
        <h3 className="text-slate-200 font-semibold text-sm">Evolução do Saldo</h3>
        <p className="text-slate-500 text-xs mt-0.5">Saldo acumulado dia a dia no mês</p>
      </div>

      <ResponsiveContainer width="100%" height={220}>
        <LineChart data={data} margin={{ top: 5, right: 10, left: 10, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
          <XAxis
            dataKey="date"
            tickFormatter={formatDate}
            tick={{ fill: '#64748b', fontSize: 11 }}
            axisLine={{ stroke: '#1e293b' }}
            tickLine={false}
            interval="preserveStartEnd"
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
            labelStyle={{ color: '#94a3b8', marginBottom: 4 }}
            labelFormatter={(label: any) => formatDate(String(label))}
            formatter={(value: any, name: any) => [
              formatCurrency(Number(value)),
              name === 'balance' ? 'Saldo' : name === 'income' ? 'Receita' : 'Despesa',
            ]}
          />
          <Legend
            formatter={(value: any) =>
              value === 'balance' ? 'Saldo' : value === 'income' ? 'Receita' : 'Despesa'
            }
            wrapperStyle={{ fontSize: 12, color: '#94a3b8' }}
          />
          <Line
            type="monotone"
            dataKey="balance"
            stroke="#6366f1"
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 4, fill: '#6366f1' }}
          />
          <Line
            type="monotone"
            dataKey="income"
            stroke="#10b981"
            strokeWidth={1.5}
            dot={false}
            strokeDasharray="4 4"
            activeDot={{ r: 3, fill: '#10b981' }}
          />
          <Line
            type="monotone"
            dataKey="expenses"
            stroke="#ef4444"
            strokeWidth={1.5}
            dot={false}
            strokeDasharray="4 4"
            activeDot={{ r: 3, fill: '#ef4444' }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}