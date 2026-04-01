import { useState } from 'react'
import { ChevronLeft, ChevronRight, ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'

interface MonthYearPickerProps {
  month:      number
  year:       number
  onChange:   (month: number, year: number) => void
  maxMonth?:  number
  maxYear?:   number
  colorClass?: string
}

const MONTHS_SHORT = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
]

const MONTHS_FULL = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
]

export default function MonthYearPicker({
  month,
  year,
  onChange,
  maxMonth,
  maxYear,
  colorClass = 'text-slate-200',
}: MonthYearPickerProps) {
  const [showPicker, setShowPicker] = useState(false)
  const [pickerYear, setPickerYear] = useState(year)

  const currentYear = new Date().getFullYear()

  function isDisabled(m: number, y: number) {
    if (!maxMonth || !maxYear) return false
    return y > maxYear || (y === maxYear && m > maxMonth)
  }

  function handlePrevMonth() {
    const date = new Date(year, month - 2)
    onChange(date.getMonth() + 1, date.getFullYear())
  }

  function handleNextMonth() {
    const date      = new Date(year, month)
    const nextMonth = date.getMonth() + 1
    const nextYear  = date.getFullYear()
    if (isDisabled(nextMonth, nextYear)) return
    onChange(nextMonth, nextYear)
  }

  function handleSelectMonth(m: number) {
    if (isDisabled(m, pickerYear)) return
    onChange(m, pickerYear)
    setShowPicker(false)
  }

  function handlePrevYear() {
    setPickerYear(y => y - 1)
  }

  function handleNextYear() {
    if (pickerYear >= currentYear) return
    setPickerYear(y => y + 1)
  }

  const isAtMax = maxMonth && maxYear
    ? year === maxYear && month === maxMonth
    : false

  return (
    <div className="relative">
      <div className="flex items-center justify-between bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5 gap-2">
        {/* Seta esquerda */}
        <button
          onClick={handlePrevMonth}
          className="p-1 text-slate-400 hover:text-slate-200 transition-colors flex-shrink-0"
        >
          <ChevronLeft size={16} />
        </button>

        {/* Label clicável */}
        <button
          onClick={() => {
            setPickerYear(year)
            setShowPicker(v => !v)
          }}
          className={cn(
            'flex items-center gap-1.5 text-sm font-medium transition-colors hover:opacity-80',
            colorClass
          )}
        >
          {MONTHS_FULL[month - 1]} {year}
          <ChevronDown
            size={14}
            className={cn(
              'transition-transform duration-200 text-slate-400',
              showPicker && 'rotate-180'
            )}
          />
        </button>

        {/* Seta direita */}
        <button
          onClick={handleNextMonth}
          disabled={!!isAtMax}
          className="p-1 text-slate-400 hover:text-slate-200 disabled:opacity-30 disabled:cursor-not-allowed transition-colors flex-shrink-0"
        >
          <ChevronRight size={16} />
        </button>
      </div>

      {/* Picker dropdown */}
      {showPicker && (
        <div className="absolute z-50 top-full mt-2 left-0 right-0 bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl p-4 min-w-[240px]">

          {/* Seletor de ano */}
          <div className="flex items-center justify-between mb-3">
            <button
              onClick={handlePrevYear}
              className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-all"
            >
              <ChevronLeft size={14} />
            </button>
            <span className="text-slate-200 font-semibold text-sm">{pickerYear}</span>
            <button
              onClick={handleNextYear}
              disabled={pickerYear >= currentYear}
              className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg disabled:opacity-30 disabled:cursor-not-allowed transition-all"
            >
              <ChevronRight size={14} />
            </button>
          </div>

          {/* Grid de meses */}
          <div className="grid grid-cols-3 gap-1.5">
            {MONTHS_SHORT.map((label, i) => {
              const m         = i + 1
              const disabled  = isDisabled(m, pickerYear)
              const isSelected = m === month && pickerYear === year

              return (
                <button
                  key={m}
                  onClick={() => handleSelectMonth(m)}
                  disabled={disabled}
                  className={cn(
                    'py-1.5 rounded-lg text-xs font-medium transition-all',
                    isSelected
                      ? 'bg-indigo-600 text-white'
                      : disabled
                        ? 'text-slate-600 cursor-not-allowed'
                        : 'text-slate-300 hover:bg-slate-800 hover:text-slate-100'
                  )}
                >
                  {label}
                </button>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}