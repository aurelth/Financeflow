import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import LoginPage from '@/features/auth/pages/LoginPage'

// Mock do hook useLogin
vi.mock('@/features/auth/api/useAuth', () => ({
  useLogin: () => ({
    mutate: vi.fn(),
    isPending: false,
  }),
}))

const renderLoginPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('LoginPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o formulário de login', () => {
    renderLoginPage()
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/senha/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /entrar/i })).toBeInTheDocument()
  })

  it('deve mostrar erros de validação com campos vazios', async () => {
    renderLoginPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /entrar/i }))

    await waitFor(() => {
      expect(screen.getByText(/email inválido/i)).toBeInTheDocument()
      expect(screen.getByText(/senha obrigatória/i)).toBeInTheDocument()
    })
  })

  it('deve mostrar erro com email inválido', async () => {
    renderLoginPage()
    const user = userEvent.setup()

    // Usa getByPlaceholderText para evitar conflito com validação nativa do browser
    const emailInput = screen.getByPlaceholderText('seu@email.com')
    await user.type(emailInput, 'email-sem-arroba')

    // Força o submit ignorando validação nativa do browser
    const form = emailInput.closest('form')!
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))

    await waitFor(() => {
      expect(screen.getByText(/email inválido/i)).toBeInTheDocument()
    })
  })

  it('deve mostrar link para registro', () => {
    renderLoginPage()
    expect(screen.getByText(/criar conta/i)).toBeInTheDocument()
  })

  it('deve alternar visibilidade da senha', async () => {
    renderLoginPage()
    const user = userEvent.setup()

    const passwordInput = screen.getByLabelText(/senha/i)
    expect(passwordInput).toHaveAttribute('type', 'password')

    // Clica no botão de mostrar senha
    const toggleBtn = passwordInput.parentElement?.querySelector('button')
    if (toggleBtn) await user.click(toggleBtn)

    expect(passwordInput).toHaveAttribute('type', 'text')
  })
})