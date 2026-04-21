import { useState } from 'react'
import { Plus, Target, Loader2 } from 'lucide-react'
import { useGoals } from '../api/useGoals'
import GoalCard from '../components/GoalCard'
import GoalModal, { type GoalModalState } from '../components/GoalModal'
import GoalsSummaryBar from '../components/GoalsSummaryBar'

export default function GoalsPage() {
  const [modal, setModal] = useState<GoalModalState>(null)
  const { data, isLoading } = useGoals()

  const goals    = data?.goals ?? []
  const isEmpty  = !isLoading && goals.length === 0

  const active    = goals.filter(g => !g.isCompleted)
  const completed = goals.filter(g => g.isCompleted)

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            Metas Financeiras
          </h1>
          <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Defina objetivos e acompanhe o progresso com base na sua poupança real
          </p>
        </div>
        <button
          onClick={() => setModal({ type: 'create' })}
          className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
          onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
        >
          <Plus size={16} />
          Nova meta
        </button>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
        </div>
      )}

      {!isLoading && (
        <div className="space-y-6">

          {/* Resumo do mês */}
          {goals.length > 0 && data && (
            <GoalsSummaryBar
              available={data.availableThisMonth}
              committed={data.committedThisMonth}
              difference={data.difference}
            />
          )}

          {/* Empty state */}
          {isEmpty && (
            <div
              className="rounded-2xl p-12 flex flex-col items-center gap-4"
              style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
            >
              <div
                className="w-16 h-16 rounded-2xl flex items-center justify-center"
                style={{ background: 'rgba(16,185,129,0.1)' }}
              >
                <Target size={28} style={{ color: 'var(--ff-emerald)' }} />
              </div>
              <div className="text-center">
                <p className="font-semibold mb-1" style={{ color: 'var(--ff-text-primary)' }}>
                  Nenhuma meta definida
                </p>
                <p className="text-sm max-w-sm" style={{ color: 'var(--ff-text-muted)' }}>
                  Cria a tua primeira meta financeira e começa a acompanhar o progresso automaticamente.
                </p>
              </div>
              <button
                onClick={() => setModal({ type: 'create' })}
                className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors"
                style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
                onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
                onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
              >
                <Plus size={15} />
                Criar primeira meta
              </button>
            </div>
          )}

          {/* Metas ativas */}
          {active.length > 0 && (
            <div>
              <h2
                className="text-xs font-semibold uppercase tracking-wide mb-3"
                style={{ color: 'var(--ff-text-muted)' }}
              >
                Em andamento — {active.length} {active.length === 1 ? 'meta' : 'metas'}
              </h2>
              <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
                {active.map(goal => (
                  <GoalCard
                    key={goal.id}
                    goal={goal}
                    onEdit={g => setModal({ type: 'edit',   goal: g })}
                    onDelete={g => setModal({ type: 'delete', goal: g })}
                  />
                ))}
              </div>
            </div>
          )}

          {/* Metas concluídas */}
          {completed.length > 0 && (
            <div>
              <h2
                className="text-xs font-semibold uppercase tracking-wide mb-3"
                style={{ color: 'var(--ff-text-muted)' }}
              >
                Concluídas — {completed.length} {completed.length === 1 ? 'meta' : 'metas'}
              </h2>
              <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
                {completed.map(goal => (
                  <GoalCard
                    key={goal.id}
                    goal={goal}
                    onEdit={g => setModal({ type: 'edit',   goal: g })}
                    onDelete={g => setModal({ type: 'delete', goal: g })}
                  />
                ))}
              </div>
            </div>
          )}

        </div>
      )}

      <GoalModal state={modal} onClose={() => setModal(null)} />
    </div>
  )
}