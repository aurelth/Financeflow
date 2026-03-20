import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import RegisterPage from '@/features/auth/pages/RegisterPage'

vi.mock('@/features/auth/api/useAuth', () => ({
  useRegister: () => ({
    mutate: vi.fn(),
    isPending: false,
  }),
}))

const renderRegisterPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('RegisterPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o formulário de registro', () => {
    renderRegisterPage()
    expect(screen.getByLabelText(/nome completo/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/^senha$/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirmar senha/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /criar conta/i })).toBeInTheDocument()
  })

  it('deve mostrar erros de validação com campos vazios', async () => {
    renderRegisterPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /criar conta/i }))

    await waitFor(() => {
      expect(screen.getByText(/nome deve ter pelo menos/i)).toBeInTheDocument()
      expect(screen.getByText(/email inválido/i)).toBeInTheDocument()
    })
  })

  it('deve mostrar erro quando senhas não coincidem', async () => {
    renderRegisterPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/nome completo/i), 'Aurel Teste')
    await user.type(screen.getByLabelText(/email/i), 'aurel@teste.com')
    await user.type(screen.getByLabelText(/^senha$/i), 'Teste@123')
    await user.type(screen.getByLabelText(/confirmar senha/i), 'Diferente@123')
    await user.click(screen.getByRole('button', { name: /criar conta/i }))

    await waitFor(() => {
      expect(screen.getByText(/senhas não coincidem/i)).toBeInTheDocument()
    })
  })

  it('deve mostrar erro com senha fraca', async () => {
    renderRegisterPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/^senha$/i), 'fraca')
    await user.click(screen.getByRole('button', { name: /criar conta/i }))

    await waitFor(() => {
      expect(screen.getByText(/mínimo 8 caracteres/i)).toBeInTheDocument()
    })
  })

  it('deve mostrar link para login', () => {
    renderRegisterPage()
    expect(screen.getByText(/já tem uma conta/i)).toBeInTheDocument()
    expect(screen.getByText(/entrar/i)).toBeInTheDocument()
  })
})