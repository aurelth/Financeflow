interface GoalsSummaryBarProps {
  available:  number
  committed:  number
  difference: number
}

export default function GoalsSummaryBar({ available, committed, difference }: GoalsSummaryBarProps) {
  const isPositive = difference >= 0
  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  return (
    <div
      className="rounded-2xl p-5 grid grid-cols-1 sm:grid-cols-3 gap-4"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex flex-col gap-1">
        <span className="text-xs uppercase tracking-wide" style={{ color: 'var(--ff-text-muted)' }}>
          Poupança disponível
        </span>
        <span className="text-lg font-semibold" style={{ color: 'var(--ff-income)' }}>
          {fmt(available)}
        </span>
        <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>este mês</span>
      </div>

      <div className="flex flex-col gap-1">
        <span className="text-xs uppercase tracking-wide" style={{ color: 'var(--ff-text-muted)' }}>
          Comprometido com metas
        </span>
        <span className="text-lg font-semibold" style={{ color: 'var(--ff-expense)' }}>
          {fmt(committed)}
        </span>
        <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>soma das contribuições</span>
      </div>

      <div className="flex flex-col gap-1">
        <span className="text-xs uppercase tracking-wide" style={{ color: 'var(--ff-text-muted)' }}>
          Diferença
        </span>
        <span
          className="text-lg font-semibold"
          style={{ color: isPositive ? 'var(--ff-income)' : 'var(--ff-expense)' }}
        >
          {isPositive ? '+' : ''}{fmt(difference)}
        </span>
        <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
          {isPositive ? 'poupança livre' : 'déficit este mês'}
        </span>
      </div>
    </div>
  )
}