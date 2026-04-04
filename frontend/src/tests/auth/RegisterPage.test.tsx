import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import RegisterPage from '@/features/auth/pages/RegisterPage'

const mockMutate = vi.fn()

vi.mock('@/features/auth/api/useAuth', () => ({
  useRegister: () => ({
    mutate: mockMutate,
    isPending: false,
  }),
}))

const renderPage = () => {
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

  it('deve renderizar todos os campos do formulário', () => {
    renderPage()
    expect(screen.getByLabelText(/nome completo/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/cpf/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/gênero/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/^senha$/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirmar senha/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /criar conta/i })).toBeInTheDocument()
  })

  it('deve exibir link para login', () => {
    renderPage()
    expect(screen.getByText(/já tem uma conta/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /entrar/i })).toBeInTheDocument()
  })

  it('deve exibir erros de validação ao submeter formulário vazio', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /criar conta/i }))

    await waitFor(() => {
      expect(document.querySelectorAll('.text-red-400').length).toBeGreaterThan(0)
    })

    expect(mockMutate).not.toHaveBeenCalled()
  })

  it('deve exibir erro quando CPF inválido', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/cpf/i), '123.456.789-00')
    await user.click(screen.getByRole('button', { name: /criar conta/i }))

    await waitFor(() => {
      expect(screen.getByText(/cpf inválido/i)).toBeInTheDocument()
    })
  })

  it('deve aplicar máscara no CPF ao digitar', async () => {
    renderPage()
    const user = userEvent.setup()

    const cpfInput = screen.getByLabelText(/cpf/i)
    await user.type(cpfInput, '52998224725')

    await waitFor(() => {
      expect((cpfInput as HTMLInputElement).value).toBe('529.982.247-25')
    })
  })

  it('deve exibir erro quando senhas não coincidem', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/nome completo/i), 'Aurel Teste')
    await user.type(screen.getByLabelText(/cpf/i), '52998224725')
    await user.selectOptions(screen.getByLabelText(/gênero/i), 'Male')
    await user.type(screen.getByLabelText(/email/i), 'aurel@teste.com')
    await user.type(screen.getByLabelText(/^senha$/i), 'Senha@123')
    await user.type(screen.getByLabelText(/confirmar senha/i), 'Diferente@123')
    await user.click(screen.getByRole('button', { name: /criar conta/i }))

    await waitFor(() => {
      expect(screen.getByText(/as senhas não coincidem/i)).toBeInTheDocument()
    }, { timeout: 10000 })
  }, 15000)

  it('deve chamar mutate com dados corretos ao submeter formulário válido', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/nome completo/i), 'Aurel Teste')
    await user.type(screen.getByLabelText(/cpf/i), '52998224725')
    await user.selectOptions(screen.getByLabelText(/gênero/i), 'Male')
    await user.type(screen.getByLabelText(/email/i), 'aurel@teste.com')
    await user.type(screen.getByLabelText(/^senha$/i), 'Senha@123')
    await user.type(screen.getByLabelText(/confirmar senha/i), 'Senha@123')
    await user.click(screen.getByRole('button', { name: /criar conta/i }))

    await waitFor(() => {
      expect(mockMutate).toHaveBeenCalledWith({
        name: 'Aurel Teste',
        cpf: '529.982.247-25',
        gender: 'Male',
        email: 'aurel@teste.com',
        password: 'Senha@123',
        currency: 'BRL',
        timezone: 'America/Sao_Paulo',
      })
    }, { timeout: 10000 })
  }, 15000)
})