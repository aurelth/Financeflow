import { useState } from 'react'
import { GitCompare, Loader2 } from 'lucide-react'
import { usePeriodComparison } from '../api/useDashboard'
import PeriodSelector from '../components/PeriodSelector'
import PeriodSummaryCards from '../components/PeriodSummaryCards'
import ComparisonBarChart from '../components/ComparisonBarChart'
import ComparisonLineChart from '../components/ComparisonLineChart'
import ComparisonTable from '../components/ComparisonTable'

interface Period {
  month: number
  year:  number
}

function getCurrentAndPrevious(): Period[] {
  const now  = new Date()
  const prev = new Date(now.getFullYear(), now.getMonth() - 1)
  return [
    { month: prev.getMonth() + 1, year: prev.getFullYear() },
    { month: now.getMonth() + 1,  year: now.getFullYear()  },
  ]
}

export default function ComparisonPage() {
  const [periods, setPeriods] = useState<Period[]>(getCurrentAndPrevious)

  const periodsQuery = periods.map(p =>
    `${p.year}-${String(p.month).padStart(2, '0')}`
  )

  const { data, isLoading } = usePeriodComparison(
    { periods: periodsQuery },
    periods.length > 0
  )

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div>
        <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
          Comparativo Histórico
        </h1>
        <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          Compare até 3 períodos e analise a evolução das suas finanças
        </p>
      </div>

      {/* Seletor de períodos */}
      <div
        className="rounded-2xl p-5 space-y-3"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <div className="flex items-center gap-2">
          <GitCompare size={16} style={{ color: 'var(--ff-emerald)' }} /> {/* Modificado: verde */}
          <span className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
            Períodos selecionados
          </span>
        </div>
        <PeriodSelector periods={periods} onChange={setPeriods} maxPeriods={3} />
      </div>

      {/* Loading spinner verde */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
        </div>
      )}

      {!isLoading && data && (
        <div className="space-y-6">
          <PeriodSummaryCards periods={data.periods} />
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
            <ComparisonLineChart periods={data.periods} />
            <ComparisonBarChart categories={data.categoryComparisons} periods={data.periods} />
          </div>
          <ComparisonTable categories={data.categoryComparisons} periods={data.periods} />
        </div>
      )}

      {/* Estado vazio */}
      {!isLoading && !data && periods.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 space-y-3">
          <div
            className="w-14 h-14 rounded-2xl flex items-center justify-center"
            style={{ background: 'var(--ff-bg-card)' }}
          >
            <GitCompare size={24} style={{ color: 'var(--ff-text-muted)' }} />
          </div>
          <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Selecione pelo menos um período para comparar
          </p>
        </div>
      )}
    </div>
  )
}