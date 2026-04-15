import {
  PieChart, Pie, Cell,
  Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import { useState } from 'react'
import { ChevronDown, ChevronRight } from 'lucide-react'
import type { ReportByCategoryDto, CategoryReportItemDto } from '../../types/analytics.types'

interface Props { data: ReportByCategoryDto }

const formatCurrency = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

const FALLBACK_COLORS = [
  '#6366f1', '#10b981', '#f59e0b', '#f43f5e', '#3b82f6',
  '#8b5cf6', '#ec4899', '#14b8a6', '#f97316', '#84cc16',
]

function CategoryRow({ item, index }: { item: CategoryReportItemDto; index: number }) {
  const [expanded, setExpanded] = useState(false)
  const color = item.categoryColor || FALLBACK_COLORS[index % FALLBACK_COLORS.length]
  const hasSubcategories = item.subcategories.length > 0

  return (
    <>
      <div
        className="grid px-4 py-3 items-center text-sm cursor-pointer transition-colors"
        style={{
          gridTemplateColumns: '2fr 1fr 1fr 1fr 32px',
          background: 'var(--ff-bg-card)',
        }}
        onClick={() => hasSubcategories && setExpanded(e => !e)}
        onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
        onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-bg-card)')}
      >
        <div className="flex items-center gap-2">
          <span
            className="w-3 h-3 rounded-full flex-shrink-0"
            style={{ background: color }}
          />
          <span style={{ color: 'var(--ff-text-primary)' }}>
            {item.categoryIcon} {item.categoryName}
          </span>
        </div>
        <span className="text-right" style={{ color: item.type === 'Expense' ? '#f87171' : '#34d399' }}>
          {formatCurrency(item.amount)}
        </span>
        <span className="text-right" style={{ color: 'var(--ff-text-muted)' }}>
          {item.percentage.toFixed(1)}%
        </span>
        <span className="text-right" style={{ color: 'var(--ff-text-muted)' }}>
          {item.transactionCount} transações
        </span>
        <span className="flex justify-end" style={{ color: 'var(--ff-text-muted)' }}>
          {hasSubcategories && (expanded
            ? <ChevronDown size={14} />
            : <ChevronRight size={14} />
          )}
        </span>
      </div>

      {/* Subcategorias */}
      {expanded && item.subcategories.map(sub => (
        <div
          key={sub.subcategoryId}
          className="grid px-4 py-2.5 text-xs"
          style={{
            gridTemplateColumns: '2fr 1fr 1fr 1fr 32px',
            background: 'var(--ff-bg-elevated)',
            borderTop: '1px solid var(--ff-border)',
          }}
        >
          <span className="pl-6" style={{ color: 'var(--ff-text-muted)' }}>
            └ {sub.subcategoryName}
          </span>
          <span className="text-right" style={{ color: item.type === 'Expense' ? '#f87171' : '#34d399' }}>
            {formatCurrency(sub.amount)}
          </span>
          <span className="text-right" style={{ color: 'var(--ff-text-muted)' }}>
            {sub.percentage.toFixed(1)}%
          </span>
          <span className="text-right" style={{ color: 'var(--ff-text-muted)' }}>
            {sub.transactionCount} transações
          </span>
          <span />
        </div>
      ))}
    </>
  )
}

export default function ByCategoryTab({ data }: Props) {
  const isEmpty = data.categories.length === 0

  const pieData = data.categories.slice(0, 10).map((c, i) => ({
    name:       `${c.categoryIcon} ${c.categoryName}`,
    value:      c.amount,
    color:      c.categoryColor || FALLBACK_COLORS[i % FALLBACK_COLORS.length],
    percentage: c.percentage,
  }))

  return (
    <div className="space-y-4">

      {/* Cards de sumário */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div
          className="rounded-2xl p-4"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <p className="text-xs uppercase tracking-wide mb-1" style={{ color: 'var(--ff-text-muted)' }}>
            Total Despesas
          </p>
          <p className="text-lg font-semibold" style={{ color: '#f87171' }}>
            {formatCurrency(data.totalExpenses)}
          </p>
        </div>
        <div
          className="rounded-2xl p-4"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <p className="text-xs uppercase tracking-wide mb-1" style={{ color: 'var(--ff-text-muted)' }}>
            Total Receitas
          </p>
          <p className="text-lg font-semibold" style={{ color: '#34d399' }}>
            {formatCurrency(data.totalIncome)}
          </p>
        </div>
      </div>

      {isEmpty ? (
        <div
          className="rounded-2xl p-10 flex flex-col items-center justify-center gap-3"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Nenhuma transação no período seleccionado
          </p>
        </div>
      ) : (
        <>
          {/* Gráfico de donut */}
          <div
            className="rounded-2xl p-5"
            style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
          >
            <div className="mb-4">
              <h3 className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
                Distribuição por Categoria
              </h3>
              <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
                Top 10 categorias no período
              </p>
            </div>
            <ResponsiveContainer width="100%" height={260}>
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%"
                  cy="50%"
                  innerRadius={65}
                  outerRadius={100}
                  paddingAngle={3}
                  dataKey="value"
                  nameKey="name"
                >
                  {pieData.map((entry, i) => (
                    <Cell key={i} fill={entry.color} />
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

          {/* Tabela por categoria com subcategorias expansíveis */}
          <div
            className="rounded-2xl overflow-hidden"
            style={{ border: '1px solid var(--ff-border)' }}
          >
            <div
              className="grid px-4 py-2.5 text-xs font-medium"
              style={{
                gridTemplateColumns: '2fr 1fr 1fr 1fr 32px',
                background: 'var(--ff-bg-elevated)',
                color: 'var(--ff-text-muted)',
              }}
            >
              <span>Categoria</span>
              <span className="text-right">Valor</span>
              <span className="text-right">%</span>
              <span className="text-right">Transações</span>
              <span />
            </div>
            <div className="divide-y" style={{ borderColor: 'var(--ff-border)' }}>
              {data.categories.map((item, i) => (
                <CategoryRow key={item.categoryId} item={item} index={i} />
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  )
}