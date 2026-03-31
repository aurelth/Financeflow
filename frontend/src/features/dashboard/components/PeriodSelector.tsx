import { Plus, X, ChevronLeft, ChevronRight } from 'lucide-react'

interface Period {
  month: number
  year:  number
}

interface PeriodSelectorProps {
  periods:   Period[]
  onChange:  (periods: Period[]) => void
  maxPeriods?: number
}

const MONTHS = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
]

const COLORS = ['bg-indigo-500/20 border-indigo-500/40 text-indigo-400',
                 'bg-emerald-500/20 border-emerald-500/40 text-emerald-400',
                 'bg-amber-500/20 border-amber-500/40 text-amber-400']

function PeriodPicker({
  period,
  color,
  onPrevMonth,
  onNextMonth,
  onRemove,
  canRemove,
}: {
  period:       Period
  color:        string
  onPrevMonth:  () => void
  onNextMonth:  () => void
  onRemove:     () => void
  canRemove:    boolean
}) {
  return (
    <div className={`flex items-center gap-2 px-3 py-2 rounded-xl border ${color}`}>
      <button
        onClick={onPrevMonth}
        className="p-0.5 hover:opacity-70 transition-opacity"
      >
        <ChevronLeft size={14} />
      </button>

      <span className="text-sm font-medium min-w-[120px] text-center">
        {MONTHS[period.month - 1]} {period.year}
      </span>

      <button
        onClick={onNextMonth}
        className="p-0.5 hover:opacity-70 transition-opacity"
      >
        <ChevronRight size={14} />
      </button>

      {canRemove && (
        <button
          onClick={onRemove}
          className="p-0.5 hover:opacity-70 transition-opacity ml-1"
        >
          <X size={12} />
        </button>
      )}
    </div>
  )
}

export default function PeriodSelector({
  periods,
  onChange,
  maxPeriods = 3,
}: PeriodSelectorProps) {
  function handlePrevMonth(index: number) {
    const updated = [...periods]
    const date    = new Date(updated[index].year, updated[index].month - 2)
    updated[index] = { month: date.getMonth() + 1, year: date.getFullYear() }
    onChange(updated)
  }

  function handleNextMonth(index: number) {
    const updated = [...periods]
    const date    = new Date(updated[index].year, updated[index].month)
    updated[index] = { month: date.getMonth() + 1, year: date.getFullYear() }
    onChange(updated)
  }

  function handleRemove(index: number) {
    onChange(periods.filter((_, i) => i !== index))
  }

  function handleAdd() {
    if (periods.length >= maxPeriods) return
    const last = periods[periods.length - 1]
    const date = new Date(last.year, last.month - 2)
    onChange([...periods, { month: date.getMonth() + 1, year: date.getFullYear() }])
  }

  return (
    <div className="flex flex-wrap items-center gap-3">
      {periods.map((period, index) => (
        <PeriodPicker
          key={index}
          period={period}
          color={COLORS[index]}
          onPrevMonth={() => handlePrevMonth(index)}
          onNextMonth={() => handleNextMonth(index)}
          onRemove={() => handleRemove(index)}
          canRemove={periods.length > 1}
        />
      ))}

      {periods.length < maxPeriods && (
        <button
          onClick={handleAdd}
          className="flex items-center gap-1.5 px-3 py-2 rounded-xl border border-dashed border-slate-600 text-slate-500 hover:text-slate-300 hover:border-slate-500 text-sm transition-all"
        >
          <Plus size={14} />
          Adicionar período
        </button>
      )}
    </div>
  )
}