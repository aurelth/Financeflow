import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import SettingsPage from '@/features/settings/pages/SettingsPage'

const mockUpdatePrefs  = vi.fn()
const mockLogoutAll    = vi.fn()
const mockUpdateProfile = vi.fn()

const mockPrefs = {
  budgetWarningEnabled:          true,
  budgetCriticalEnabled:         true,
  transactionDueTomorrowEnabled: true,
  transactionDueIn3DaysEnabled:  true,
}

vi.mock('@/features/settings/api/useSettings', () => ({
  useNotificationPreferences: () => ({
    data:      mockPrefs,
    isLoading: false,
  }),
  useUpdateNotificationPreferences: () => ({
    mutate:    mockUpdatePrefs,
    isPending: false,
  }),
  useLogoutAll: () => ({
    mutate:    mockLogoutAll,
    isPending: false,
  }),
  useDeleteAccount: () => ({
    mutate:    vi.fn(),
    isPending: false,
  }),
}))

// Mock do useUpdateProfile
vi.mock('@/features/auth/api/useAuth', () => ({
  useUpdateProfile: () => ({
    mutate:    mockUpdateProfile,
    isPending: false,
  }),
}))

// Mock do authStore
vi.mock('@/store/authStore', () => ({
  useAuthStore: (selector: any) => selector({
    user: {
      id:       '123',
      name:     'Aurel Teste',
      email:    'aurel@teste.com',
      currency: 'BRL',
      timezone: 'America/Sao_Paulo',
      language: 'en-US',
      role:     'User',
    },
  }),
}))

// Mock do i18n
vi.mock('@/lib/i18n', () => ({
  default: { changeLanguage: vi.fn(), language: 'en-US' },
  SUPPORTED_LANGUAGES: ['pt-BR', 'en-US', 'es-ES', 'fr-FR'],
  LANGUAGE_LABELS: {
    'pt-BR': 'Português (Brasil)',
    'en-US': 'English (US)',
    'es-ES': 'Español',
    'fr-FR': 'Français',
  },
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <SettingsPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('SettingsPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument()
  })

  it('deve renderizar a secção de idioma', () => {
    renderPage()
    expect(screen.getByText('Português (Brasil)')).toBeInTheDocument()
    expect(screen.getByText('English (US)')).toBeInTheDocument()
    expect(screen.getByText('Español')).toBeInTheDocument()
    expect(screen.getByText('Français')).toBeInTheDocument()
  })

  it('deve renderizar os 4 toggles de notificação', () => {
    renderPage()
    expect(screen.getByText('Aviso de orçamento (80%)')).toBeInTheDocument()
    expect(screen.getByText('Limite de orçamento atingido (100%)')).toBeInTheDocument()
    expect(screen.getByText('Transação vence amanhã')).toBeInTheDocument()
    expect(screen.getByText('Transação vence em 3 dias')).toBeInTheDocument()
  })

  it('deve marcar o idioma activo com ✓', () => {
    renderPage()
    // en-US está activo no mock
    const checkmark = screen.getByText('✓')
    expect(checkmark).toBeInTheDocument()
  })

  it('deve chamar updatePrefs ao clicar num toggle', async () => {
    renderPage()
    const user = userEvent.setup()

    const toggles = screen.getAllByRole('button').filter(btn =>
      btn.className.includes('rounded-full')
    )

    await user.click(toggles[0])

    await waitFor(() => {
      expect(mockUpdatePrefs).toHaveBeenCalledWith(
        expect.objectContaining({ budgetWarningEnabled: false })
      )
    })
  })

  it('deve chamar logoutAll ao clicar em encerrar sessões', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /encerrar todas as sessões/i }))

    expect(mockLogoutAll).toHaveBeenCalledOnce()
  })

  it('deve abrir o modal de exclusão ao clicar em excluir conta', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /excluir conta/i }))

    expect(screen.getByText(/esta ação é permanente/i)).toBeInTheDocument()
  })

  it('deve chamar updateProfile ao seleccionar idioma', async () => {
    renderPage()
    const user = userEvent.setup()

    const ptButton = screen.getByRole('button', { name: /português/i })
    await user.click(ptButton)

    await waitFor(() => {
      expect(mockUpdateProfile).toHaveBeenCalledWith(
        expect.objectContaining({ language: 'pt-BR' })
      )
    })
  })
})