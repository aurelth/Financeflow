import {
  PieChart, Pie, Cell,
  Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import type { ExpensesByCategory } from '../types/dashboard.types'

interface PieChartCardProps {
  data: ExpensesByCategory[]
}

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function PieChartCard({ data }: PieChartCardProps) {
  if (data.length === 0) {
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
            Distribuição das despesas do mês
          </p>
        </div>
        <div className="flex items-center justify-center h-[220px]">
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Nenhuma despesa no período
          </p>
        </div>
      </div>
    )
  }

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
          Distribuição das despesas do mês
        </p>
      </div>

      <ResponsiveContainer width="100%" height={220}>
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius={55}
            outerRadius={85}
            paddingAngle={3}
            dataKey="total"
            nameKey="categoryName"
          >
            {data.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={entry.categoryColor} />
            ))}
          </Pie>
          <Tooltip
            contentStyle={{
              backgroundColor: '#111111',
              border: '1px solid #222222',
              borderRadius: '12px',
              fontSize: '12px',
              color: '#a1a1aa',
            }}
            formatter={(value: any, name: any) => [formatCurrency(Number(value)), name]}
          />
          <Legend
            formatter={(value: any, entry: any) =>
              `${value} (${entry?.payload?.percentage?.toFixed(1) ?? 0}%)`
            }
            wrapperStyle={{ fontSize: 11, color: '#52525b' }}
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}