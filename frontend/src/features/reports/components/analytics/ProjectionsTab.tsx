import {
  ComposedChart, Bar, Line, XAxis, YAxis,
  CartesianGrid, Tooltip, ResponsiveContainer, Legend,
  ReferenceLine,
} from 'recharts'
import type { ProjectionsDto, ProjectionMonthDto } from '../../types/analytics.types'

interface Props { data: ProjectionsDto }

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

const formatK = (v: number) =>
  Math.abs(v) >= 1000 ? `R$ ${(v / 1000).toFixed(0)}k` : `R$ ${v.toFixed(0)}`

function MonthRow({ m, isProjected }: { m: ProjectionMonthDto; isProjected: boolean }) {
  return (
    <div
      className="grid px-4 py-3 text-sm"
      style={{
        gridTemplateColumns: '1.5fr 1fr 1fr 1fr',
        background: isProjected ? 'rgba(99,102,241,0.04)' : 'var(--ff-bg-card)',
      }}
    >
      <div className="flex items-center gap-2">
        <span style={{ color: 'var(--ff-text-primary)' }}>
          {m.monthName}/{m.year}
        </span>
        {isProjected && (
          <span
            className="px-1.5 py-0.5 rounded text-xs"
            style={{ background: 'rgba(99,102,241,0.15)', color: '#818cf8' }}
          >
            proj.
          </span>
        )}
      </div>
      <span className="text-right" style={{ color: '#34d399' }}>{formatCurrency(m.income)}</span>
      <span className="text-right" style={{ color: '#f87171' }}>{formatCurrency(m.expenses)}</span>
      <span className="text-right" style={{ color: m.balance >= 0 ? '#34d399' : '#f87171' }}>
        {formatCurrency(m.balance)}
      </span>
    </div>
  )
}

export default function ProjectionsTab({ data }: Props) {
  // Combina histórico + projecções para o gráfico
  const allMonths = [...data.historical, ...data.projected]

  const chartData = allMonths.map(m => ({
    name:            `${m.monthName.substring(0, 3)}/${m.year}`,
    Receitas:        !m.isProjected ? m.income    : undefined,
    Despesas:        !m.isProjected ? m.expenses  : undefined,
    'Rec. Projectada': m.isProjected ? m.income   : undefined,
    'Desp. Projectada': m.isProjected ? m.expenses : undefined,
    Saldo:           m.balance,
    isProjected:     m.isProjected,
  }))

  // Índice onde começa a projecção — para a linha de referência
  const projectionStartIndex = data.historical.length

  // Médias do histórico para referência
  const avgIncome   = data.historical.length > 0
    ? data.historical.reduce((s, m) => s + m.income, 0)   / data.historical.length
    : 0
  const avgExpenses = data.historical.length > 0
    ? data.historical.reduce((s, m) => s + m.expenses, 0) / data.historical.length
    : 0

  return (
    <div className="space-y-4">

      {/* Cards de contexto */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div
          className="rounded-2xl p-4"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <p className="text-xs uppercase tracking-wide mb-1" style={{ color: 'var(--ff-text-muted)' }}>
            Meses analisados
          </p>
          <p className="text-lg font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            {data.monthsAnalysed}
          </p>
        </div>
        <div
          className="rounded-2xl p-4"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <p className="text-xs uppercase tracking-wide mb-1" style={{ color: 'var(--ff-text-muted)' }}>
            Meses projectados
          </p>
          <p className="text-lg font-semibold" style={{ color: '#818cf8' }}>
            {data.monthsAhead}
          </p>
        </div>
        <div
          className="rounded-2xl p-4"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <p className="text-xs uppercase tracking-wide mb-1" style={{ color: 'var(--ff-text-muted)' }}>
            Média rec. histórica
          </p>
          <p className="text-lg font-semibold" style={{ color: '#34d399' }}>
            {formatCurrency(avgIncome)}
          </p>
        </div>
        <div
          className="rounded-2xl p-4"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <p className="text-xs uppercase tracking-wide mb-1" style={{ color: 'var(--ff-text-muted)' }}>
            Média desp. histórica
          </p>
          <p className="text-lg font-semibold" style={{ color: '#f87171' }}>
            {formatCurrency(avgExpenses)}
          </p>
        </div>
      </div>

      {/* Nota explicativa */}
      <div
        className="flex items-start gap-3 px-4 py-3 rounded-xl text-xs"
        style={{ background: 'rgba(99,102,241,0.08)', border: '1px solid rgba(99,102,241,0.2)', color: '#a5b4fc' }}
      >
        <span className="text-base">✦</span>
        <span>
          As projecções são calculadas com <strong>regressão linear ponderada</strong> (meses recentes têm mais peso)
          combinada com <strong>ajuste sazonal</strong> (mesmo mês em anos anteriores).
          Os valores projectados são estimativas — não garantias.
        </span>
      </div>

      {/* Gráfico combinado histórico + projecção */}
      <div
        className="rounded-2xl p-5"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="mb-4">
          <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
            Histórico + Projecção
          </h3>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Barras sólidas = histórico real · Barras tracejadas = projecção
          </p>
        </div>

        <ResponsiveContainer width="100%" height={320}>
          <ComposedChart data={chartData} margin={{ top: 5, right: 10, left: 10, bottom: 5 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#1a1a1a" vertical={false} />
            <XAxis
              dataKey="name"
              tick={{ fill: '#52525b', fontSize: 10 }}
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
              formatter={(value: any, name: any) => [
                value !== undefined ? formatCurrency(Number(value)) : '—',
                name,
              ]}
            />
            <Legend wrapperStyle={{ fontSize: 11, color: '#52525b' }} />

            {/* Linha de separação histórico/projecção */}
            {projectionStartIndex > 0 && projectionStartIndex < chartData.length && (
              <ReferenceLine
                yAxisId="bars"
                x={chartData[projectionStartIndex]?.name}
                stroke="#6366f1"
                strokeDasharray="4 4"
                strokeWidth={1.5}
                label={{ value: 'Projecção →', fill: '#818cf8', fontSize: 10, position: 'top' }}
              />
            )}

            {/* Barras históricas */}
            <Bar yAxisId="bars" dataKey="Receitas"          fill="#34d399" radius={[4,4,0,0]} maxBarSize={24} />
            <Bar yAxisId="bars" dataKey="Despesas"          fill="#f87171" radius={[4,4,0,0]} maxBarSize={24} />

            {/* Barras projectadas — mais transparentes */}
            <Bar yAxisId="bars" dataKey="Rec. Projectada"   fill="#34d399" fillOpacity={0.4} radius={[4,4,0,0]} maxBarSize={24} />
            <Bar yAxisId="bars" dataKey="Desp. Projectada"  fill="#f87171" fillOpacity={0.4} radius={[4,4,0,0]} maxBarSize={24} />

            {/* Linha de saldo */}
            <Line
              yAxisId="line"
              type="monotone"
              dataKey="Saldo"
              stroke="#6366f1"
              strokeWidth={2}
              strokeDasharray="0"
              dot={(props: any) => {
                const { cx, cy, payload } = props
                return (
                  <circle
                    key={`dot-${cx}-${cy}`}
                    cx={cx}
                    cy={cy}
                    r={payload.isProjected ? 5 : 4}
                    fill={payload.isProjected ? '#818cf8' : '#6366f1'}
                    stroke={payload.isProjected ? '#6366f1' : 'none'}
                    strokeWidth={payload.isProjected ? 1.5 : 0}
                    strokeDasharray={payload.isProjected ? '3 2' : '0'}
                  />
                )
              }}
            />
          </ComposedChart>
        </ResponsiveContainer>
      </div>

      {/* Tabela combinada */}
      <div
        className="rounded-2xl overflow-hidden"
        style={{ border: '1px solid var(--ff-border)' }}
      >
        <div
          className="grid px-4 py-2.5 text-xs font-medium"
          style={{
            gridTemplateColumns: '1.5fr 1fr 1fr 1fr',
            background: 'var(--ff-bg-elevated)',
            color: 'var(--ff-text-muted)',
          }}
        >
          <span>Mês</span>
          <span className="text-right">Receitas</span>
          <span className="text-right">Despesas</span>
          <span className="text-right">Saldo</span>
        </div>
        <div className="divide-y" style={{ borderColor: 'var(--ff-border)' }}>
          {data.historical.map(m => (
            <MonthRow key={`h-${m.year}-${m.month}`} m={m} isProjected={false} />
          ))}
          {data.projected.map(m => (
            <MonthRow key={`p-${m.year}-${m.month}`} m={m} isProjected={true} />
          ))}
        </div>
      </div>
    </div>
  )
}