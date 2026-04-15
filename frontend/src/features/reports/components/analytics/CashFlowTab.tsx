import {
  ComposedChart, Bar, Line, XAxis, YAxis,
  CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import type { CashFlowDto } from '../../types/analytics.types'

interface Props { data: CashFlowDto }

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

const formatK = (v: number) =>
  Math.abs(v) >= 1000 ? `R$ ${(v / 1000).toFixed(0)}k` : `R$ ${v.toFixed(0)}`

function SummaryCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div
      className="rounded-2xl p-4 flex flex-col gap-1"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <p className="text-xs uppercase tracking-wide" style={{ color: 'var(--ff-text-muted)' }}>{label}</p>
      <p className="text-lg font-semibold" style={{ color }}>
        {formatCurrency(value)}
      </p>
    </div>
  )
}

export default function CashFlowTab({ data }: Props) {
  const chartData = data.periods.map(p => ({
    name:              p.label,
    Receitas:          p.income,
    Despesas:          p.expenses,
    'Saldo Acumulado': p.cumulativeBalance,
  }))

  return (
    <div className="space-y-4">

      {/* Cards de sumário */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <SummaryCard label="Total Receitas"  value={data.totalIncome}   color="#34d399" />
        <SummaryCard label="Total Despesas"  value={data.totalExpenses} color="#f87171" />
        <SummaryCard label="Saldo Líquido"   value={data.netBalance}    color={data.netBalance >= 0 ? '#34d399' : '#f87171'} />
      </div>

      {/* Gráfico combinado — barras + linha */}
      <div
        className="rounded-2xl p-5"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="mb-4">
          <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
            Fluxo de Caixa
          </h3>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Receitas e despesas por {data.groupBy === 'day' ? 'dia' : 'mês'} com saldo acumulado
          </p>
        </div>

        <ResponsiveContainer width="100%" height={300}>
          <ComposedChart data={chartData} margin={{ top: 5, right: 10, left: 10, bottom: 5 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#1a1a1a" vertical={false} />
            <XAxis
              dataKey="name"
              tick={{ fill: '#52525b', fontSize: 11 }}
              axisLine={{ stroke: '#222222' }}
              tickLine={false}
            />
            <YAxis
              yAxisId="bars"
              tickFormatter={formatK}
              tick={{ fill: '#52525b', fontSize: 11 }}
              axisLine={false}
              tickLine={false}
              width={60}
            />
            <YAxis
              yAxisId="line"
              orientation="right"
              tickFormatter={formatK}
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
            <Bar yAxisId="bars" dataKey="Receitas"  fill="#34d399" radius={[4, 4, 0, 0]} maxBarSize={32} />
            <Bar yAxisId="bars" dataKey="Despesas"  fill="#f87171" radius={[4, 4, 0, 0]} maxBarSize={32} />
            <Line
              yAxisId="line"
              type="monotone"
              dataKey="Saldo Acumulado"
              stroke="#6366f1"
              strokeWidth={2}
              dot={{ fill: '#6366f1', r: 4 }}
              activeDot={{ r: 6 }}
            />
          </ComposedChart>
        </ResponsiveContainer>
      </div>

      {/* Tabela de períodos */}
      <div
        className="rounded-2xl overflow-hidden"
        style={{ border: '1px solid var(--ff-border)' }}
      >
        <div
          className="grid grid-cols-5 px-4 py-2.5 text-xs font-medium"
          style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-muted)', gridTemplateColumns: '1fr 1fr 1fr 1fr 1fr' }}
        >
          <span>Período</span>
          <span className="text-right">Receitas</span>
          <span className="text-right">Despesas</span>
          <span className="text-right">Saldo</span>
          <span className="text-right">Acumulado</span>
        </div>
        <div className="divide-y" style={{ borderColor: 'var(--ff-border)' }}>
          {data.periods.map((p, i) => (
            <div
              key={i}
              className="grid grid-cols-5 px-4 py-3 text-sm"
              style={{ gridTemplateColumns: '1fr 1fr 1fr 1fr 1fr', background: 'var(--ff-bg-card)' }}
            >
              <span style={{ color: 'var(--ff-text-primary)' }}>{p.label}</span>
              <span className="text-right" style={{ color: '#34d399' }}>{formatCurrency(p.income)}</span>
              <span className="text-right" style={{ color: '#f87171' }}>{formatCurrency(p.expenses)}</span>
              <span className="text-right" style={{ color: p.balance >= 0 ? '#34d399' : '#f87171' }}>
                {formatCurrency(p.balance)}
              </span>
              <span className="text-right" style={{ color: p.cumulativeBalance >= 0 ? '#34d399' : '#f87171' }}>
                {formatCurrency(p.cumulativeBalance)}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}