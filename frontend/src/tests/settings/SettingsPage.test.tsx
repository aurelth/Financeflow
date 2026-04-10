import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import SettingsPage from '@/features/settings/pages/SettingsPage'

const mockUpdatePrefs = vi.fn()
const mockLogoutAll   = vi.fn()

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
    expect(screen.getByText('Configurações')).toBeInTheDocument()
  })

  it('deve renderizar as secções principais', () => {
    renderPage()
    expect(screen.getByText('Notificações')).toBeInTheDocument()
    expect(screen.getByText('Sessão')).toBeInTheDocument()
    expect(screen.getByText('Zona de perigo')).toBeInTheDocument()
  })

  it('deve renderizar os 4 toggles de notificação', () => {
    renderPage()
    expect(screen.getByText('Aviso de orçamento (80%)')).toBeInTheDocument()
    expect(screen.getByText('Limite de orçamento atingido (100%)')).toBeInTheDocument()
    expect(screen.getByText('Transação vence amanhã')).toBeInTheDocument()
    expect(screen.getByText('Transação vence em 3 dias')).toBeInTheDocument()
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
})