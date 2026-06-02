import { useState } from 'react'
import { Pencil, Trash2, CalendarClock, TrendingUp, History } from 'lucide-react'
import type { GoalProgressResultDto } from '../types/goal.types'
import GoalContributionsModal from './GoalContributionsModal'

interface GoalCardProps {
  goal:     GoalProgressResultDto
  onEdit:   (goal: GoalProgressResultDto) => void
  onDelete: (goal: GoalProgressResultDto) => void
}

function getStatusStyle(status: GoalProgressResultDto['status']): { color: string; bg: string; label: string } {
  switch (status) {
    case 'OnTrack':   return { color: 'var(--ff-income)',    bg: 'rgba(16,185,129,0.1)',  label: 'Em dia'     }
    case 'Behind':    return { color: 'var(--ff-pending)',   bg: 'rgba(245,158,11,0.1)',  label: 'Atrasada'   }
    case 'Completed': return { color: 'var(--ff-emerald)',   bg: 'rgba(16,185,129,0.15)', label: 'Concluída'  }
    case 'Overdue':   return { color: 'var(--ff-expense)',   bg: 'rgba(244,63,94,0.1)',   label: 'Vencida'    }
  }
}

function getProgressColor(percentage: number): string {
  if (percentage >= 80) return '#10b981'
  if (percentage >= 50) return '#6366f1'
  if (percentage >= 25) return '#f59e0b'
  return '#f43f5e'
}

function fmt(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function GoalCard({ goal, onEdit, onDelete }: GoalCardProps) {
  const [showContributions, setShowContributions] = useState(false)

  const statusStyle   = getStatusStyle(goal.status)
  const progressColor = goal.isCompleted ? '#10b981' : getProgressColor(goal.progressPercentage)
  const percentage    = goal.progressPercentage
  const radius        = 54
  const circumference = 2 * Math.PI * radius
  const offset        = circumference - (percentage / 100) * circumference
  const deadlineDate  = new Date(`${goal.deadline}Z`)
  const deadlineLabel = deadlineDate.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' })

  return (
    <>
      <div
        className="rounded-2xl p-5 flex flex-col gap-4 transition-all duration-200"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
        onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--ff-border-subtle)')}
        onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
      >
        {/* Cabeçalho */}
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div
              className="w-11 h-11 rounded-xl flex items-center justify-center text-2xl flex-shrink-0"
              style={{ background: 'var(--ff-bg-elevated)' }}
            >
              {goal.emoji}
            </div>
            <div>
              <p className="font-semibold text-sm" style={{ color: 'var(--ff-text-primary)' }}>
                {goal.name}
              </p>
              <div className="flex items-center gap-1.5 mt-0.5">
                <CalendarClock size={11} style={{ color: 'var(--ff-text-muted)' }} />
                <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                  Prazo: {deadlineLabel}
                </span>
              </div>
            </div>
          </div>

          {/* Ações */}
          {!goal.isCompleted && (
            <div className="flex items-center gap-1">
              <button
                onClick={() => onEdit(goal)}
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
                title="Editar"
              >
                <Pencil size={14} />
              </button>
              <button
                onClick={() => onDelete(goal)}
                className="p-1.5 rounded-lg transition-all"
                style={{ color: 'var(--ff-text-muted)' }}
                onMouseEnter={e => {
                  e.currentTarget.style.color      = 'var(--ff-expense)'
                  e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                }}
                onMouseLeave={e => {
                  e.currentTarget.style.color      = 'var(--ff-text-muted)'
                  e.currentTarget.style.background = 'transparent'
                }}
                title="Remover"
              >
                <Trash2 size={14} />
              </button>
            </div>
          )}
        </div>

        {/* Progresso circular */}
        <div className="flex items-center gap-4">
          <div className="relative flex-shrink-0">
            <svg width="100" height="100" viewBox="0 0 128 128">
              <circle cx="64" cy="64" r={radius} fill="none" stroke="var(--ff-border)" strokeWidth="10" />
              {percentage > 0 && (
                <circle
                  cx="64" cy="64" r={radius}
                  fill="none"
                  stroke={progressColor}
                  strokeWidth="10"
                  strokeLinecap="round"
                  strokeDasharray={circumference}
                  strokeDashoffset={offset}
                  transform="rotate(-90 64 64)"
                  style={{ transition: 'stroke-dashoffset 0.5s ease' }}
                />
              )}
              <text x="64" y="58" textAnchor="middle" fontSize="20" fontWeight="700" fill={progressColor}>
                {Math.round(percentage)}%
              </text>
              <text x="64" y="74" textAnchor="middle" fontSize="11" fill="var(--ff-text-muted)">
                concluído
              </text>
            </svg>
          </div>

          <div className="flex flex-col gap-2 flex-1">
            <div>
              <p className="text-xs mb-0.5" style={{ color: 'var(--ff-text-muted)' }}>Acumulado</p>
              <p className="text-sm font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
                {fmt(goal.accumulatedAmount)}
                <span className="text-xs font-normal ml-1" style={{ color: 'var(--ff-text-muted)' }}>
                  / {fmt(goal.targetAmount)}
                </span>
              </p>
            </div>

            {/* Contribuição do mês */}
            {!goal.isCompleted && (
              <div
                className="rounded-xl px-3 py-2"
                style={{ background: 'var(--ff-bg-elevated)' }}
              >
                <p className="text-xs mb-1" style={{ color: 'var(--ff-text-muted)' }}>
                  Este mês
                </p>
                <div className="flex items-center justify-between gap-2 flex-wrap">
                  <span className="text-xs" style={{ color: 'var(--ff-text-secondary)' }}>
                    Planejado: {fmt(goal.plannedThisMonth)}
                  </span>
                  <span
                    className="text-xs font-medium"
                    style={{ color: goal.receivedThisMonth >= goal.plannedThisMonth ? 'var(--ff-income)' : 'var(--ff-expense)' }}
                  >
                    Recebido: {fmt(goal.receivedThisMonth)}
                  </span>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Rodapé */}
        <div className="flex items-center justify-between flex-wrap gap-y-2">
          <span
            className="text-xs font-medium px-2.5 py-1 rounded-full"
            style={{ background: statusStyle.bg, color: statusStyle.color }}
          >
            {statusStyle.label}
          </span>

          <div className="flex items-center gap-3 flex-wrap">
            {/* Botão de contribuições */}
            {goal.linkedCategoryId && (
              <button
                onClick={() => setShowContributions(true)}
                className="flex items-center gap-1 text-xs transition-colors"
                style={{ color: 'var(--ff-text-muted)' }}
                onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
                onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
              >
                <History size={12} />
                Contribuições
              </button>
            )}

            {!goal.isCompleted && goal.monthsToComplete !== null && (
              <div className="flex items-center gap-1">
                <TrendingUp size={12} style={{ color: 'var(--ff-text-muted)' }} />
                <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                  ~{goal.monthsToComplete} {goal.monthsToComplete === 1 ? 'mês' : 'meses'} restantes
                </span>
              </div>
            )}

            {goal.isCompleted && (
              <span className="text-xs" style={{ color: 'var(--ff-emerald)' }}>🎉 Meta concluída!</span>
            )}
          </div>
        </div>
      </div>

      {/* Modal de contribuições */}
      {showContributions && (
        <GoalContributionsModal
          goal={goal}
          onClose={() => setShowContributions(false)}
        />
      )}
    </>
  )
}