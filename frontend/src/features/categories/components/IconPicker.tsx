import { cn } from '@/lib/utils'

const ICONS = [
  '🍔', '🚗', '🏠', '💊', '📚', '🎮', '✈️', '👕',
  '💡', '📱', '🎵', '🏋️', '🐶', '☕', '🎁', '💼',
  '💰', '📈', '🏦', '🛒', '🎬', '🍕', '⚽', '🌿',
]

interface IconPickerProps {
  value:    string
  onChange: (icon: string) => void
}

export default function IconPicker({ value, onChange }: IconPickerProps) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {ICONS.map(icon => (
        <button
          key={icon}
          type="button"
          onClick={() => onChange(icon)}
          className={cn(
            'w-9 h-9 rounded-lg text-lg flex items-center justify-center transition-all duration-200',
            value === icon
              ? 'bg-indigo-500/20 ring-1 ring-indigo-500/50 scale-110'
              : 'bg-slate-800 hover:bg-slate-700'
          )}
        >
          {icon}
        </button>
      ))}
    </div>
  )
}