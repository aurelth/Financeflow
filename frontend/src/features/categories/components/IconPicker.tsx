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
          className="w-9 h-9 rounded-lg text-lg flex items-center justify-center transition-all duration-200"
          style={value === icon
            ? { background: 'var(--ff-emerald-subtle)', outline: '1px solid rgba(16,185,129,0.4)', transform: 'scale(1.1)' }
            : { background: 'var(--ff-bg-elevated)' }
          }
          onMouseEnter={e => { if (value !== icon) e.currentTarget.style.background = '#222222' }}
          onMouseLeave={e => { if (value !== icon) e.currentTarget.style.background = 'var(--ff-bg-elevated)' }}
        >
          {icon}
        </button>
      ))}
    </div>
  )
}