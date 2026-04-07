interface ColorPickerProps {
  value:    string
  onChange: (color: string) => void
}

const COLORS = [
  '#6366f1', '#8b5cf6', '#ec4899', '#ef4444',
  '#f97316', '#eab308', '#22c55e', '#10b981',
  '#14b8a6', '#06b6d4', '#3b82f6', '#64748b',
]

export default function ColorPicker({ value, onChange }: ColorPickerProps) {
  return (
    <div className="flex flex-wrap gap-2">
      {COLORS.map(color => (
        <button
          key={color}
          type="button"
          onClick={() => onChange(color)}
          className="w-7 h-7 rounded-lg transition-all duration-200"
          style={{
            backgroundColor: color,
            outline:         value === color ? `2px solid ${color}` : 'none',
            outlineOffset:   value === color ? '2px' : '0',
            transform:       value === color ? 'scale(1.15)' : 'scale(1)',
          }}
          title={color}
        />
      ))}
    </div>
  )
}