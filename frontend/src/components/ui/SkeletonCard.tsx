import { motion } from 'framer-motion'

interface SkeletonCardProps {
  lines?:  number
  height?: string
}

export default function SkeletonCard({ lines = 3, height = 'h-32' }: SkeletonCardProps) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className={`rounded-2xl p-5 ${height}`}
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="animate-pulse space-y-3">
        <div className="h-3 rounded-full w-1/3" style={{ background: 'var(--ff-bg-elevated)' }} />
        {Array.from({ length: lines - 1 }).map((_, i) => (
          <div
            key={i}
            className="h-3 rounded-full"
            style={{
              background: 'var(--ff-bg-elevated)',
              width: `${60 + (i % 3) * 15}%`,
            }}
          />
        ))}
      </div>
    </motion.div>
  )
}