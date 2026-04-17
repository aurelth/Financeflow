import { Loader2 } from 'lucide-react'

interface ButtonSpinnerProps {
  loading:   boolean
  children:  React.ReactNode
  loadingText?: string
}

export default function ButtonSpinner({ loading, children, loadingText }: ButtonSpinnerProps) {
  return loading ? (
    <span className="flex items-center gap-2">
      <Loader2 size={15} className="animate-spin" />
      {loadingText ?? children}
    </span>
  ) : (
    <>{children}</>
  )
}