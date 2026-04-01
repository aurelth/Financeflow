import { Plus, X } from 'lucide-react'
import MonthYearPicker from '@/components/ui/MonthYearPicker'

interface Period {
  month: number
  year:  number
}

interface PeriodSelectorProps {
  periods:    Period[]
  onChange:   (periods: Period[]) => void
  maxPeriods?: number
}

const COLORS: Array<{ colorClass: string }> = [
  { colorClass: 'text-indigo-400'  },
  { colorClass: 'text-emerald-400' },
  { colorClass: 'text-amber-400'   },
]

export default function PeriodSelector({
  periods,
  onChange,
  maxPeriods = 3,
}: PeriodSelectorProps) {
  function handleChange(index: number, month: number, year: number) {
    const updated   = [...periods]
    updated[index]  = { month, year }
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
    <div className="flex flex-wrap items-start gap-3">
      {periods.map((period, index) => (
        <div key={index} className="flex items-start gap-2">
          <div className="flex-1 min-w-[200px]">
            <MonthYearPicker
              month={period.month}
              year={period.year}
              onChange={(m, y) => handleChange(index, m, y)}
              colorClass={COLORS[index].colorClass}
            />
          </div>
          {periods.length > 1 && (
            <button
              onClick={() => handleRemove(index)}
              className="mt-2.5 p-1 text-slate-500 hover:text-red-400 hover:bg-slate-800 rounded-lg transition-all"
            >
              <X size={14} />
            </button>
          )}
        </div>
      ))}

      {periods.length < maxPeriods && (
        <button
          onClick={handleAdd}
          className="flex items-center gap-1.5 px-3 py-2.5 rounded-xl border border-dashed border-slate-600 text-slate-500 hover:text-slate-300 hover:border-slate-500 text-sm transition-all"
        >
          <Plus size={14} />
          Adicionar período
        </button>
      )}
    </div>
  )
}