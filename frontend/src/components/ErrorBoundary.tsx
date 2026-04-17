import { Component, type ReactNode, type ErrorInfo } from 'react'
import { AlertTriangle, RefreshCw, Home } from 'lucide-react'

interface Props {
  children:  ReactNode
  fallback?: ReactNode
}

interface State {
  hasError: boolean
  error:    Error | null
}

export default class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Em desenvolvimento, loga o erro detalhado
    if (import.meta.env.DEV) {
      console.error('[ErrorBoundary]', error, info.componentStack)
    }
  }

  handleReset() {
    this.setState({ hasError: false, error: null })
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback

      return (
        <div className="flex flex-col items-center justify-center min-h-[400px] px-6 text-center">
          <div
            className="w-16 h-16 rounded-2xl flex items-center justify-center mb-4"
            style={{ background: 'rgba(244,63,94,0.1)', border: '1px solid rgba(244,63,94,0.2)' }}
          >
            <AlertTriangle size={28} style={{ color: 'var(--ff-expense)' }} />
          </div>

          <h2 className="text-lg font-semibold mb-2" style={{ color: 'var(--ff-text-primary)' }}>
            Algo correu mal
          </h2>

          <p className="text-sm mb-6 max-w-sm" style={{ color: 'var(--ff-text-muted)' }}>
            Ocorreu um erro inesperado nesta secção. Podes tentar recarregar ou voltar ao Dashboard.
          </p>

          {import.meta.env.DEV && this.state.error && (
            <details className="mb-6 text-left w-full max-w-sm">
              <summary
                className="text-xs cursor-pointer mb-2"
                style={{ color: 'var(--ff-text-muted)' }}
              >
                Detalhes do erro (dev)
              </summary>
              <pre
                className="text-xs p-3 rounded-xl overflow-auto"
                style={{
                  background: 'var(--ff-bg-elevated)',
                  color:      'var(--ff-expense)',
                  border:     '1px solid var(--ff-border)',
                }}
              >
                {this.state.error.message}
              </pre>
            </details>
          )}

          <div className="flex gap-3">
            <button
              onClick={() => this.handleReset()}
              className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors"
              style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-secondary)', border: '1px solid var(--ff-border)' }}
              onMouseEnter={e => {
                e.currentTarget.style.background = 'var(--ff-border)'
                e.currentTarget.style.color      = 'var(--ff-text-primary)'
              }}
              onMouseLeave={e => {
                e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                e.currentTarget.style.color      = 'var(--ff-text-secondary)'
              }}
            >
              <RefreshCw size={15} />
              Tentar novamente
            </button>

            <button
              onClick={() => { this.handleReset(); window.location.href = '/dashboard' }}
              className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors"
              style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
              onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
            >
              <Home size={15} />
              Ir ao Dashboard
            </button>
          </div>
        </div>
      )
    }

    return this.props.children
  }
}