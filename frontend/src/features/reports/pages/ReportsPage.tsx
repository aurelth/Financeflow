import { useState } from 'react'
import {
  TrendingUp, TrendingDown, BarChart3, Tag, Sparkles, Loader2,
  ChevronLeft, ChevronRight,
} from 'lucide-react'
import { useCashFlow, useAnnualSummary, useReportByCategory, useReportByTag, useProjections } from '../api/useAnalytics'
import CashFlowTab      from '../components/analytics/CashFlowTab'
import AnnualSummaryTab from '../components/analytics/AnnualSummaryTab'
import ByCategoryTab    from '../components/analytics/ByCategoryTab'
import ByTagTab         from '../components/analytics/ByTagTab'
import ProjectionsTab   from '../components/analytics/ProjectionsTab'

// ─── Helpers ────────────────────────────────────────────────────────────────

function toDateStr(date: Date) {
  return date.toISOString().split('T')[0]
}

function getDefaultRange() {
  const now   = new Date()
  const from  = new Date(now.getFullYear(), 0, 1)          // 1 Jan do ano actual
  const to    = new Date(now.getFullYear(), now.getMonth() + 1, 0) // fim do mês actual
  return { from: toDateStr(from), to: toDateStr(to) }
}

// ─── Tabs ────────────────────────────────────────────────────────────────────

const TABS = [
  { id: 'cash-flow',       label: 'Fluxo de Caixa',  icon: TrendingUp  },
  { id: 'annual',          label: 'Resumo Anual',     icon: BarChart3   },
  { id: 'by-category',     label: 'Por Categoria',    icon: TrendingDown },
  { id: 'by-tag',          label: 'Por Tag',          icon: Tag         },
  { id: 'projections',     label: 'Projecções',       icon: Sparkles    },
] as const

type TabId = typeof TABS[number]['id']

// ─── Page ────────────────────────────────────────────────────────────────────

export default function ReportsPage() {
  const [activeTab, setActiveTab] = useState<TabId>('cash-flow')
  const [range, setRange]         = useState(getDefaultRange)
  const [groupBy, setGroupBy]     = useState<'day' | 'month'>('month')
  const [year, setYear]           = useState(new Date().getFullYear())
  const [categoryType, setCategoryType] = useState<'Income' | 'Expense' | undefined>('Expense')

  // ── Queries ──────────────────────────────────────────────────────────────
  const { data: cashFlow,    isLoading: l1 } = useCashFlow(
    { from: range.from, to: range.to, groupBy },
    activeTab === 'cash-flow')

  const { data: annualSummary, isLoading: l2 } = useAnnualSummary(
    year,
    activeTab === 'annual')

  const { data: byCategory,  isLoading: l3 } = useReportByCategory(
    { from: range.from, to: range.to, type: categoryType },
    activeTab === 'by-category')

  const { data: byTag,       isLoading: l4 } = useReportByTag(
    { from: range.from, to: range.to },
    activeTab === 'by-tag')

  const { data: projections, isLoading: l5 } = useProjections(
    { monthsBack: 12, monthsAhead: 3 },
    activeTab === 'projections')

  const isLoading = l1 || l2 || l3 || l4 || l5

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div>
        <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
          Relatórios
        </h1>
        <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          Analise as suas finanças em detalhe
        </p>
      </div>

      {/* Tabs */}
      <div
        className="flex items-center gap-1 p-1 rounded-2xl overflow-x-auto"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        {TABS.map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            onClick={() => setActiveTab(id)}
            className="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium transition-all whitespace-nowrap flex-shrink-0"
            style={activeTab === id
              ? { background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }
              : { color: 'var(--ff-text-muted)' }
            }
            onMouseEnter={e => {
              if (activeTab !== id) {
                e.currentTarget.style.color      = 'var(--ff-text-primary)'
                e.currentTarget.style.background = 'var(--ff-bg-elevated)'
              }
            }}
            onMouseLeave={e => {
              if (activeTab !== id) {
                e.currentTarget.style.color      = 'var(--ff-text-muted)'
                e.currentTarget.style.background = 'transparent'
              }
            }}
          >
            <Icon size={15} />
            {label}
          </button>
        ))}
      </div>

      {/* Filtros comuns — período */}
      {activeTab !== 'annual' && activeTab !== 'projections' && (
        <div
          className="flex flex-wrap items-center gap-4 px-5 py-4 rounded-2xl"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <div className="flex items-center gap-2">
            <label className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>De</label>
            <input
              type="date"
              value={range.from}
              onChange={e => setRange(r => ({ ...r, from: e.target.value }))}
              className="text-sm rounded-lg px-3 py-1.5 outline-none"
              style={{
                background: 'var(--ff-bg-elevated)',
                color: 'var(--ff-text-primary)',
                border: '1px solid var(--ff-border)',
              }}
            />
          </div>
          <div className="flex items-center gap-2">
            <label className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>Até</label>
            <input
              type="date"
              value={range.to}
              onChange={e => setRange(r => ({ ...r, to: e.target.value }))}
              className="text-sm rounded-lg px-3 py-1.5 outline-none"
              style={{
                background: 'var(--ff-bg-elevated)',
                color: 'var(--ff-text-primary)',
                border: '1px solid var(--ff-border)',
              }}
            />
          </div>

          {/* Agrupamento — só no fluxo de caixa */}
          {activeTab === 'cash-flow' && (
            <div className="flex items-center gap-1 rounded-xl p-1" style={{ background: 'var(--ff-bg-elevated)' }}>
              {(['day', 'month'] as const).map(g => (
                <button
                  key={g}
                  onClick={() => setGroupBy(g)}
                  className="px-3 py-1 rounded-lg text-xs font-medium transition-all"
                  style={groupBy === g
                    ? { background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }
                    : { color: 'var(--ff-text-muted)' }
                  }
                >
                  {g === 'day' ? 'Dia' : 'Mês'}
                </button>
              ))}
            </div>
          )}

          {/* Tipo — só em por categoria */}
          {activeTab === 'by-category' && (
            <div className="flex items-center gap-1 rounded-xl p-1" style={{ background: 'var(--ff-bg-elevated)' }}>
              {([undefined, 'Expense', 'Income'] as const).map(t => (
                <button
                  key={String(t)}
                  onClick={() => setCategoryType(t)}
                  className="px-3 py-1 rounded-lg text-xs font-medium transition-all"
                  style={categoryType === t
                    ? { background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }
                    : { color: 'var(--ff-text-muted)' }
                  }
                >
                  {t === undefined ? 'Todos' : t === 'Expense' ? 'Despesas' : 'Receitas'}
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Filtro de ano — só no resumo anual */}
      {activeTab === 'annual' && (
        <div
          className="flex items-center justify-between px-5 py-3 rounded-2xl"
          style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        >
          <button
            onClick={() => setYear(y => y - 1)}
            className="p-1.5 rounded-lg transition-all"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => {
              e.currentTarget.style.color      = 'var(--ff-text-primary)'
              e.currentTarget.style.background = 'var(--ff-bg-elevated)'
            }}
            onMouseLeave={e => {
              e.currentTarget.style.color      = 'var(--ff-text-muted)'
              e.currentTarget.style.background = 'transparent'
            }}
          >
            <ChevronLeft size={18} />
          </button>
          <span className="font-medium" style={{ color: 'var(--ff-text-primary)' }}>{year}</span>
          <button
            onClick={() => setYear(y => y + 1)}
            className="p-1.5 rounded-lg transition-all"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => {
              e.currentTarget.style.color      = 'var(--ff-text-primary)'
              e.currentTarget.style.background = 'var(--ff-bg-elevated)'
            }}
            onMouseLeave={e => {
              e.currentTarget.style.color      = 'var(--ff-text-muted)'
              e.currentTarget.style.background = 'transparent'
            }}
          >
            <ChevronRight size={18} />
          </button>
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
        </div>
      )}

      {/* Conteúdo das tabs */}
      {!isLoading && (
        <>
          {activeTab === 'cash-flow'   && cashFlow    && <CashFlowTab      data={cashFlow} />}
          {activeTab === 'annual'      && annualSummary && <AnnualSummaryTab data={annualSummary} />}
          {activeTab === 'by-category' && byCategory  && <ByCategoryTab    data={byCategory} />}
          {activeTab === 'by-tag'      && byTag        && <ByTagTab         data={byTag} />}
          {activeTab === 'projections' && projections  && <ProjectionsTab   data={projections} />}
        </>
      )}
    </div>
  )
}