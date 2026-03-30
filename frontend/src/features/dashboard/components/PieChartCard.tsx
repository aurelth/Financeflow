import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
  Legend,
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
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
        <div className="mb-4">
          <h3 className="text-slate-200 font-semibold text-sm">Despesas por Categoria</h3>
          <p className="text-slate-500 text-xs mt-0.5">Distribuição das despesas do mês</p>
        </div>
        <div className="flex items-center justify-center h-[220px]">
          <p className="text-slate-500 text-sm">Nenhuma despesa no período</p>
        </div>
      </div>
    )
  }

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
      <div className="mb-4">
        <h3 className="text-slate-200 font-semibold text-sm">Despesas por Categoria</h3>
        <p className="text-slate-500 text-xs mt-0.5">Distribuição das despesas do mês</p>
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
              backgroundColor: '#0f172a',
              border: '1px solid #1e293b',
              borderRadius: '12px',
              fontSize: '12px',
            }}
            formatter={(value: any, name: any) => [
              formatCurrency(Number(value)),
              name,
            ]}
          />
          <Legend
            formatter={(value: any, entry: any) =>
              `${entry?.payload?.categoryIcon ?? ''} ${value} (${entry?.payload?.percentage?.toFixed(1) ?? 0}%)`
            }
            wrapperStyle={{ fontSize: 11, color: '#94a3b8' }}
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}