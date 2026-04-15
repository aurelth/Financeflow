import {
  BarChart, Bar, XAxis, YAxis,
  CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts'
import type { ReportByTagDto } from '../../types/analytics.types'
import { Cell } from 'recharts'

interface Props { data: ReportByTagDto }

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

const formatK = (v: number) =>
  Math.abs(v) >= 1000 ? `R$ ${(v / 1000).toFixed(0)}k` : `R$ ${v.toFixed(0)}`

const TAG_COLORS = [
  '#6366f1', '#10b981', '#f59e0b', '#f43f5e', '#3b82f6',
  '#8b5cf6', '#ec4899', '#14b8a6', '#f97316', '#84cc16',
]

export default function ByTagTab({ data }: Props) {
  const isEmpty = data.tags.length === 0

  const chartData = data.tags.slice(0, 15).map(t => ({
    name:  t.tag,
    Valor: t.amount,
  }))

  return (
    <div className="space-y-4">

      {/* Card de sumário */}
      <div
        className="rounded-2xl p-4"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <p className="text-xs uppercase tracking-wide mb-1" style={{ color: 'var(--ff-text-muted)' }}>
          Total por Tags
        </p>
        <p className="text-lg font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
          {formatCurrency(data.totalAmount)}
        </p>
      </div>

      {isEmpty ? (
        <div
          className="rounded-2xl p-10 flex flex-col items-center justify-center gap-3"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Nenhuma transação com tags no período seleccionado
          </p>
        </div>
      ) : (
        <>
          {/* Gráfico de barras horizontais */}
          <div
            className="rounded-2xl p-5"
            style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
          >
            <div className="mb-4">
              <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
                Distribuição por Tag
              </h3>
              <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
                Top 15 tags no período
              </p>
            </div>
            <ResponsiveContainer width="100%" height={Math.max(200, chartData.length * 40)}>
              <BarChart
                data={chartData}
                layout="vertical"
                margin={{ top: 5, right: 10, left: 60, bottom: 5 }}
              >
                <CartesianGrid strokeDasharray="3 3" stroke="#1a1a1a" horizontal={false} />
                <XAxis
                  type="number"
                  tickFormatter={formatK}
                  tick={{ fill: '#52525b', fontSize: 11 }}
                  axisLine={false}
                  tickLine={false}
                />
                <YAxis
                  type="category"
                  dataKey="name"
                  tick={{ fill: '#a1a1aa', fontSize: 11 }}
                  axisLine={false}
                  tickLine={false}
                  width={55}
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
                <Bar dataKey="Valor" radius={[0, 4, 4, 0]} maxBarSize={24}>
                  {chartData.map((_, i) => (
                    <Cell key={i} fill={TAG_COLORS[i % TAG_COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>

          {/* Tabela de tags */}
          <div
            className="rounded-2xl overflow-hidden"
            style={{ border: '1px solid var(--ff-border)' }}
          >
            <div
              className="grid px-4 py-2.5 text-xs font-medium"
              style={{
                gridTemplateColumns: '1fr 1fr 1fr 1fr',
                background: 'var(--ff-bg-elevated)',
                color: 'var(--ff-text-muted)',
              }}
            >
              <span>Tag</span>
              <span className="text-right">Valor</span>
              <span className="text-right">%</span>
              <span className="text-right">Transações</span>
            </div>
            <div className="divide-y" style={{ borderColor: 'var(--ff-border)' }}>
              {data.tags.map((t, i) => (
                <div
                  key={t.tag}
                  className="grid px-4 py-3 text-sm"
                  style={{
                    gridTemplateColumns: '1fr 1fr 1fr 1fr',
                    background: 'var(--ff-bg-card)',
                  }}
                >
                  <div className="flex items-center gap-2">
                    <span
                      className="w-2.5 h-2.5 rounded-full flex-shrink-0"
                      style={{ background: TAG_COLORS[i % TAG_COLORS.length] }}
                    />
                    <span style={{ color: 'var(--ff-text-primary)' }}>{t.tag}</span>
                  </div>
                  <span className="text-right" style={{ color: 'var(--ff-text-primary)' }}>
                    {formatCurrency(t.amount)}
                  </span>
                  <span className="text-right" style={{ color: 'var(--ff-text-muted)' }}>
                    {t.percentage.toFixed(1)}%
                  </span>
                  <span className="text-right" style={{ color: 'var(--ff-text-muted)' }}>
                    {t.transactionCount} transações
                  </span>
                </div>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  )
}