import { motion } from 'framer-motion'
import type { LucideIcon } from 'lucide-react'

interface EmptyStateProps {
  icon:        LucideIcon
  title:       string
  description: string
  action?:     {
    label:   string
    onClick: () => void
  }
}

export default function EmptyState({ icon: Icon, title, description, action }: EmptyStateProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className="flex flex-col items-center justify-center py-16 px-6 text-center"
    >
      <div
        className="w-16 h-16 rounded-2xl flex items-center justify-center mb-4"
        style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
      >
        <Icon size={28} style={{ color: 'var(--ff-text-muted)' }} />
      </div>

      <h3 className="text-base font-semibold mb-1" style={{ color: 'var(--ff-text-primary)' }}>
        {title}
      </h3>

      <p className="text-sm max-w-xs" style={{ color: 'var(--ff-text-muted)' }}>
        {description}
      </p>

      {action && (
        <button
          onClick={action.onClick}
          className="mt-5 px-5 py-2.5 rounded-xl text-sm font-medium transition-colors"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
          onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
          onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
        >
          {action.label}
        </button>
      )}
    </motion.div>
  )
}