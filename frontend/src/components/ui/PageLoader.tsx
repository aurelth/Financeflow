import { motion } from 'framer-motion'
import { Loader2 } from 'lucide-react'

interface PageLoaderProps {
  message?: string
}

export default function PageLoader({ message }: PageLoaderProps) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="flex flex-col items-center justify-center py-20 gap-3"
    >
      <Loader2
        size={28}
        className="animate-spin"
        style={{ color: 'var(--ff-emerald)' }}
      />
      {message && (
        <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
          {message}
        </p>
      )}
    </motion.div>
  )
}