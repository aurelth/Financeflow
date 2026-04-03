import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ResetPasswordPage from '@/features/auth/pages/ResetPasswordPage'

const mockMutate = vi.fn()

vi.mock('@/features/auth/api/useAuth', () => ({
  useResetPassword: () => ({
    mutate:    mockMutate,
    isPending: false,
  }),
}))

const renderPage = (token = 'valid-token') => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[`/reset-password?token=${token}`]}>
        <Routes>
          <Route path="/reset-password" element={<ResetPasswordPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

const renderPageWithoutToken = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/reset-password']}>
        <Routes>
          <Route path="/reset-password" element={<ResetPasswordPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('ResetPasswordPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o formulário quando token está presente', () => {
    renderPage()
    expect(screen.getByLabelText(/nova senha/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirmar senha/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /redefinir senha/i })).toBeInTheDocument()
  })

  it('deve exibir mensagem de erro quando token está ausente', () => {
    renderPageWithoutToken()
    expect(screen.getByText(/link de redefinição inválido/i)).toBeInTheDocument()
  })

  it('deve exibir erro quando senha é fraca', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/nova senha/i), 'fraca')
    await user.click(screen.getByRole('button', { name: /redefinir senha/i }))

    await waitFor(() => {
      expect(screen.getByText(/mínimo 8 caracteres/i)).toBeInTheDocument()
    })
  })

  it('deve exibir erro quando senhas não coincidem', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/nova senha/i), 'NovaSenha@123')
    await user.type(screen.getByLabelText(/confirmar senha/i), 'Diferente@123')
    await user.click(screen.getByRole('button', { name: /redefinir senha/i }))

    await waitFor(() => {
      expect(screen.getByText(/as senhas não coincidem/i)).toBeInTheDocument()
    })
  })

  it('deve chamar mutate com dados corretos ao submeter', async () => {
    renderPage('meu-token-valido')
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/nova senha/i), 'NovaSenha@123')
    await user.type(screen.getByLabelText(/confirmar senha/i), 'NovaSenha@123')
    await user.click(screen.getByRole('button', { name: /redefinir senha/i }))

    await waitFor(() => {
      expect(mockMutate).toHaveBeenCalledWith({
        token:           'meu-token-valido',
        newPassword:     'NovaSenha@123',
        confirmPassword: 'NovaSenha@123',
      })
    })
  })

  it('deve exibir link para voltar ao login', () => {
    renderPage()
    expect(screen.getByText(/voltar para o login/i)).toBeInTheDocument()
  })
})