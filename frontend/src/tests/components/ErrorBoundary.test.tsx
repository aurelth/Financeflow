import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ErrorBoundary from '@/components/ErrorBoundary'

// Componente que lança erro para testes
function BrokenComponent(): never {
  throw new Error('Erro de teste')
}

function WorkingComponent() {
  return <div>Componente funcional</div>
}

describe('ErrorBoundary', () => {
  // Suprime erros no console durante os testes
  beforeEach(() => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('deve renderizar os filhos quando não há erro', () => {
    render(
      <ErrorBoundary>
        <WorkingComponent />
      </ErrorBoundary>
    )
    expect(screen.getByText('Componente funcional')).toBeInTheDocument()
  })

  it('deve renderizar o fallback quando há erro', () => {
    render(
      <ErrorBoundary>
        <BrokenComponent />
      </ErrorBoundary>
    )
    expect(screen.getByText('Algo correu mal')).toBeInTheDocument()
  })

  it('deve exibir botões de acção no fallback', () => {
    render(
      <ErrorBoundary>
        <BrokenComponent />
      </ErrorBoundary>
    )
    expect(screen.getByRole('button', { name: /tentar novamente/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /ir ao dashboard/i })).toBeInTheDocument()
  })

  it('deve renderizar fallback personalizado quando fornecido', () => {
    render(
      <ErrorBoundary fallback={<div>Erro personalizado</div>}>
        <BrokenComponent />
      </ErrorBoundary>
    )
    expect(screen.getByText('Erro personalizado')).toBeInTheDocument()
  })

  it('deve recuperar ao clicar em tentar novamente', async () => {
    const user = userEvent.setup()
    let shouldThrow = true

    function ConditionalComponent() {
      if (shouldThrow) throw new Error('Erro de teste')
      return <div>Recuperado</div>
    }

    const { rerender } = render(
      <ErrorBoundary>
        <ConditionalComponent />
      </ErrorBoundary>
    )

    expect(screen.getByText('Algo correu mal')).toBeInTheDocument()

    shouldThrow = false
    await user.click(screen.getByRole('button', { name: /tentar novamente/i }))

    rerender(
      <ErrorBoundary>
        <ConditionalComponent />
      </ErrorBoundary>
    )

    expect(screen.getByText('Recuperado')).toBeInTheDocument()
  })
})