import { useNavigate } from 'react-router-dom'
import { Users, UserCheck, UserX, ShieldCheck, Tag, Loader2, ArrowRight } from 'lucide-react'
import { useAdminMetrics } from '../api/useAdmin'

interface MetricCardProps {
  label:   string
  value:   number
  icon:    React.ReactNode
  color:   string
  bg:      string
  border:  string
}

function MetricCard({ label, value, icon, color, bg, border }: MetricCardProps) {
  return (
    <div
      className="rounded-2xl p-5 flex items-center gap-4"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div
        className="w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0"
        style={{ background: bg, border }}
      >
        <span style={{ color }}>{icon}</span>
      </div>
      <div>
        <p className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>
          {value.toLocaleString('pt-BR')}
        </p>
        <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          {label}
        </p>
      </div>
    </div>
  )
}

export default function AdminDashboardPage() {
  const { data: metrics, isLoading } = useAdminMetrics()
  const navigate = useNavigate()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
      </div>
    )
  }

  const cards: MetricCardProps[] = [
    {
      label:  'Total de usuários',
      value:  metrics?.totalUsers        ?? 0,
      icon:   <Users size={22} />,
      color:  '#818cf8',
      bg:     'rgba(99,102,241,0.1)',
      border: '1px solid rgba(99,102,241,0.2)',
    },
    {
      label:  'Usuários ativos',
      value:  metrics?.activeUsers       ?? 0,
      icon:   <UserCheck size={22} />,
      color:  'var(--ff-emerald)',
      bg:     'var(--ff-emerald-subtle)',
      border: '1px solid rgba(16,185,129,0.3)',
    },
    {
      label:  'Usuários inativos',
      value:  metrics?.inactiveUsers     ?? 0,
      icon:   <UserX size={22} />,
      color:  'var(--ff-expense)',
      bg:     'rgba(244,63,94,0.1)',
      border: '1px solid rgba(244,63,94,0.2)',
    },
    {
      label:  'Admins ativos',
      value:  metrics?.totalAdmins       ?? 0,
      icon:   <ShieldCheck size={22} />,
      color:  '#f59e0b',
      bg:     'rgba(245,158,11,0.1)',
      border: '1px solid rgba(245,158,11,0.2)',
    },
    {
      label:  'Categorias padrão',
      value:  metrics?.defaultCategories ?? 0,
      icon:   <Tag size={22} />,
      color:  '#06b6d4',
      bg:     'rgba(6,182,212,0.1)',
      border: '1px solid rgba(6,182,212,0.2)',
    },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold" style={{ color: 'var(--ff-text-primary)' }}>
          Painel Administrativo
        </h1>
        <p className="text-sm mt-1" style={{ color: 'var(--ff-text-muted)' }}>
          Visão geral da plataforma
        </p>
      </div>

      {/* Cards de métricas */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {cards.map(card => (
          <MetricCard key={card.label} {...card} />
        ))}
      </div>

      {/* Ações rápidas */}
      <div
        className="rounded-2xl p-6"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        <h2 className="font-semibold mb-4" style={{ color: 'var(--ff-text-primary)' }}>
          Ações rápidas
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <button
            onClick={() => navigate('/admin/users')}
            className="flex items-center justify-between px-4 py-3 rounded-xl transition-colors text-left"
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
            onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
            onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
          >
            <div className="flex items-center gap-3">
              <Users size={16} style={{ color: 'var(--ff-emerald)' }} />
              <div>
                <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
                  Gerenciar usuários
                </p>
                <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                  {metrics?.totalUsers ?? 0} usuário(s) cadastrado(s)
                </p>
              </div>
            </div>
            <ArrowRight size={16} style={{ color: 'var(--ff-text-muted)' }} />
          </button>

          <button
            onClick={() => navigate('/admin/categories')}
            className="flex items-center justify-between px-4 py-3 rounded-xl transition-colors text-left"
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
            onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--ff-emerald)')}
            onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
          >
            <div className="flex items-center gap-3">
              <Tag size={16} style={{ color: 'var(--ff-emerald)' }} />
              <div>
                <p className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
                  Categorias padrão
                </p>
                <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                  {metrics?.defaultCategories ?? 0} categoria(s) configurada(s)
                </p>
              </div>
            </div>
            <ArrowRight size={16} style={{ color: 'var(--ff-text-muted)' }} />
          </button>
        </div>
      </div>
    </div>
  )
}