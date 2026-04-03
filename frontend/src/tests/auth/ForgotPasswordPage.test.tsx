import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ForgotPasswordPage from '@/features/auth/pages/ForgotPasswordPage'

const mockMutate = vi.fn()

vi.mock('@/features/auth/api/useAuth', () => ({
  useForgotPassword: () => ({
    mutate:    mockMutate,
    isPending: false,
  }),
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ForgotPasswordPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('ForgotPasswordPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o formulário', () => {
    renderPage()
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /enviar link/i })).toBeInTheDocument()
  })

  it('deve exibir link para voltar ao login', () => {
    renderPage()
    expect(screen.getByText(/voltar para o login/i)).toBeInTheDocument()
  })

  it('deve exibir erro de validação ao submeter com email inválido', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /enviar link/i }))

    await waitFor(() => {
      expect(document.querySelector('.text-red-400')).toBeInTheDocument()
    })

    expect(mockMutate).not.toHaveBeenCalled()
  })

  it('deve chamar mutate com email correto ao submeter', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/email/i), 'aurel@teste.com')
    await user.click(screen.getByRole('button', { name: /enviar link/i }))

    await waitFor(() => {
      expect(mockMutate).toHaveBeenCalledWith({ email: 'aurel@teste.com' })
    })
  })
})