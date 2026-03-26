import { cn } from '@/lib/utils'

const COLORS = [
  '#6366f1', '#8b5cf6', '#ec4899', '#ef4444',
  '#f97316', '#eab308', '#22c55e', '#10b981',
  '#14b8a6', '#06b6d4', '#3b82f6', '#64748b',
]

interface ColorPickerProps {
  value:    string
  onChange: (color: string) => void
}

export default function ColorPicker({ value, onChange }: ColorPickerProps) {
  return (
    <div className="flex flex-wrap gap-2">
      {COLORS.map(color => (
        <button
          key={color}
          type="button"
          onClick={() => onChange(color)}
          className={cn(
            'w-7 h-7 rounded-lg transition-all duration-200 ring-offset-2 ring-offset-slate-900',
            value === color && 'ring-2 ring-white scale-110'
          )}
          style={{ backgroundColor: color }}
          title={color}
        />
      ))}
    </div>
  )
}