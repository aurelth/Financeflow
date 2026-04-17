import { motion } from 'framer-motion'

interface SkeletonTableProps {
  rows?:    number
  columns?: number
}

export default function SkeletonTable({ rows = 5, columns = 4 }: SkeletonTableProps) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="rounded-2xl overflow-hidden"
      style={{ border: '1px solid var(--ff-border)' }}
    >
      {/* Header */}
      <div
        className="grid gap-4 px-4 py-3"
        style={{
          gridTemplateColumns: `repeat(${columns}, 1fr)`,
          background: 'var(--ff-bg-elevated)',
        }}
      >
        {Array.from({ length: columns }).map((_, i) => (
          <div
            key={i}
            className="h-3 rounded-full animate-pulse"
            style={{ background: 'var(--ff-border)', width: '60%' }}
          />
        ))}
      </div>

      {/* Rows */}
      <div className="divide-y" style={{ borderColor: 'var(--ff-border)' }}>
        {Array.from({ length: rows }).map((_, rowIdx) => (
          <div
            key={rowIdx}
            className="grid gap-4 px-4 py-4"
            style={{
              gridTemplateColumns: `repeat(${columns}, 1fr)`,
              background: 'var(--ff-bg-card)',
            }}
          >
            {Array.from({ length: columns }).map((_, colIdx) => (
              <div
                key={colIdx}
                className="h-3 rounded-full animate-pulse"
                style={{
                  background: 'var(--ff-bg-elevated)',
                  width: `${50 + ((rowIdx + colIdx) % 4) * 12}%`,
                }}
              />
            ))}
          </div>
        ))}
      </div>
    </motion.div>
  )
}