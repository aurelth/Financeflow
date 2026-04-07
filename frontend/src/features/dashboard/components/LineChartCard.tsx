import {
  LineChart, Line, XAxis, YAxis,
  CartesianGrid, Tooltip, ResponsiveContainer, Legend,
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

// Modificado: estilos do tooltip e grid com tokens da nova paleta
export default function LineChartCard({ data }: LineChartCardProps) {
  return (
    <div
      className="rounded-2xl p-5"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="mb-4">
        <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
          Evolução do Saldo
        </h3>
        <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          Saldo acumulado dia a dia no mês
        </p>
      </div>

      <ResponsiveContainer width="100%" height={220}>
        <LineChart data={data} margin={{ top: 5, right: 10, left: 10, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#1a1a1a" />
          <XAxis
            dataKey="date"
            tickFormatter={formatDate}
            tick={{ fill: '#52525b', fontSize: 11 }}
            axisLine={{ stroke: '#222222' }}
            tickLine={false}
            interval="preserveStartEnd"
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
            labelStyle={{ color: '#a1a1aa', marginBottom: 4 }}
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
            wrapperStyle={{ fontSize: 12, color: '#52525b' }}
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
            stroke="#f43f5e"
            strokeWidth={1.5}
            dot={false}
            strokeDasharray="4 4"
            activeDot={{ r: 3, fill: '#f43f5e' }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}