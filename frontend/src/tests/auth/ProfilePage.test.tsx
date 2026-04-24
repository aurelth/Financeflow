import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ProfilePage from '@/features/auth/pages/ProfilePage'

const mockUpdateProfile  = vi.fn()
const mockChangePassword = vi.fn()

// Mock do react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const keys: Record<string, string> = {
        'profile.title':           'Perfil',
        'profile.subtitle':        'Gerencie suas informações e preferências',
        'profile.identity':        'Dados pessoais',
        'profile.preferences':     'Preferências',
        'profile.security':        'Alterar senha',
        'profile.name':            'Nome completo',
        'profile.email':           'Email',
        'profile.cpf':             'CPF',
        'profile.gender':          'Gênero',
        'profile.currency':        'Moeda',
        'profile.timezone':        'Fuso horário',
        'profile.language':        'Idioma',
        'profile.currentPassword': 'Senha atual',
        'profile.newPassword':     'Nova senha',
        'profile.confirmPassword': 'Confirmar senha',
        'profile.savePreferences': 'Salvar preferências',
        'profile.changePassword':  'Alterar senha',
        'register.genderMale':     'Masculino',
        'register.genderFemale':   'Feminino',
        'common:actions.loading':  'Carregando...',
      }
      return keys[key] ?? key
    },
    i18n: { changeLanguage: vi.fn() },
  }),
}))

const mockProfile = {
  id:        '123',
  name:      'Aurel Teste',
  email:     'aurel@teste.com',
  cpf:       '529.982.247-25',
  gender:    'Male',
  currency:  'BRL',
  timezone:  'America/Sao_Paulo',
  language:  'pt-BR',
  createdAt: '2026-01-01T00:00:00Z',
  role:      'User',
}

vi.mock('@/features/auth/api/useAuth', () => ({
  useUserProfile: () => ({
    data:      mockProfile,
    isLoading: false,
  }),
  useUpdateProfile: () => ({
    mutate:    mockUpdateProfile,
    isPending: false,
  }),
  useChangePassword: () => ({
    mutate:    mockChangePassword,
    isPending: false,
  }),
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ProfilePage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('ProfilePage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar os dados de identidade somente leitura', () => {
    renderPage()
    expect(screen.getByText('Aurel Teste')).toBeInTheDocument()
    expect(screen.getByText('aurel@teste.com')).toBeInTheDocument()
    expect(screen.getByText('529.982.247-25')).toBeInTheDocument()
    expect(screen.getByText('Masculino')).toBeInTheDocument()
  })

  it('deve exibir link para contato de alteração de nome', () => {
    renderPage()
    expect(screen.getByText(/contato@financeflowapp\.io/i)).toBeInTheDocument()
  })

  it('deve renderizar o formulário de preferências', () => {
    renderPage()
    expect(screen.getByLabelText(/moeda/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/fuso horário/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/idioma/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /salvar preferências/i })).toBeInTheDocument()
  })

  it('deve renderizar o formulário de alteração de senha', () => {
    renderPage()
    expect(screen.getByLabelText(/senha atual/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/nova senha/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirmar senha/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /alterar senha/i })).toBeInTheDocument()
  })

  it('deve chamar updateProfile ao salvar preferências', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /salvar preferências/i }))

    await waitFor(() => {
      expect(mockUpdateProfile).toHaveBeenCalledWith({
        currency: 'BRL',
        timezone: 'America/Sao_Paulo',
        language: 'pt-BR',
      })
    })
  })

  it('deve exibir erro ao submeter senhas que não coincidem', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/senha atual/i), 'SenhaAtual@123')
    await user.type(screen.getByLabelText(/nova senha/i), 'NovaSenha@123')
    await user.type(screen.getByLabelText(/confirmar senha/i), 'Diferente@123')
    await user.click(screen.getByRole('button', { name: /alterar senha/i }))

    await waitFor(() => {
      expect(screen.getByText(/as senhas não coincidem/i)).toBeInTheDocument()
    })

    expect(mockChangePassword).not.toHaveBeenCalled()
  })

  it('deve chamar changePassword com dados corretos', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/senha atual/i), 'SenhaAtual@123')
    await user.type(screen.getByLabelText(/nova senha/i), 'NovaSenha@123')
    await user.type(screen.getByLabelText(/confirmar senha/i), 'NovaSenha@123')
    await user.click(screen.getByRole('button', { name: /alterar senha/i }))

    await waitFor(() => {
      expect(mockChangePassword).toHaveBeenCalledWith(
        {
          currentPassword: 'SenhaAtual@123',
          newPassword:     'NovaSenha@123',
          confirmPassword: 'NovaSenha@123',
        },
        expect.any(Object)
      )
    })
  })

  // Testes de idioma
  it('deve renderizar o seletor de idioma com 4 opções', () => {
    renderPage()
    const languageSelect = screen.getByLabelText(/idioma/i)
    expect(languageSelect).toBeInTheDocument()
    const options = languageSelect.querySelectorAll('option')
    expect(options).toHaveLength(4)
  })

  it('deve ter pt-BR como idioma pré-seleccionado', () => {
    renderPage()
    const languageSelect = screen.getByLabelText(/idioma/i) as HTMLSelectElement
    expect(languageSelect.value).toBe('pt-BR')
  })
})