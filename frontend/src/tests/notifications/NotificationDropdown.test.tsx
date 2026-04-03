import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import NotificationDropdown from '@/features/notifications/components/NotificationDropdown'
import { useNotificationStore } from '@/store/notificationStore'
import type { Notification } from '@/features/notifications/types/notification.types'

const mockNotifications: Notification[] = [
  {
    id:          'notif-1',
    type:        'BudgetWarning',
    message:     'Orçamento de Alimentação atingiu 80%.',
    isRead:      false,
    referenceId: null,
    createdAt:   '2026-03-15T10:00:00Z',
  },
  {
    id:          'notif-2',
    type:        'BudgetCritical',
    message:     'Orçamento de Transporte atingiu 100%.',
    isRead:      true,
    referenceId: null,
    createdAt:   '2026-03-14T09:00:00Z',
  },
]

const mockDueNotifications: Notification[] = [
  {
    id:          'notif-3',
    type:        'TransactionDueTomorrow',
    message:     'A transação "Aluguel" vence amanhã (05/04/2026) — R$ 1.500,00.',
    isRead:      false,
    referenceId: 'tx-1',
    createdAt:   '2026-04-04T10:00:00Z',
  },
  {
    id:          'notif-4',
    type:        'TransactionDueIn3Days',
    message:     'A transação "Internet" vence em 3 dias (07/04/2026) — R$ 120,00.',
    isRead:      false,
    referenceId: 'tx-2',
    createdAt:   '2026-04-04T10:00:00Z',
  },
]

const mockMarkAsRead    = vi.fn()
const mockMarkAllAsRead = vi.fn()

// Mock dinâmico — retorna o estado atual do store
vi.mock('@/features/notifications/api/useNotifications', () => ({
  useNotifications: () => ({
    data:      useNotificationStore.getState().notifications,
    isLoading: false,
  }),
  useMarkAsRead:    () => ({ mutate: mockMarkAsRead }),
  useMarkAllAsRead: () => ({ mutate: mockMarkAllAsRead }),
}))

const renderDropdown = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <NotificationDropdown />
    </QueryClientProvider>
  )
}

describe('NotificationDropdown', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    useNotificationStore.setState({
      notifications: [],
      unreadCount:   0,
    })
  })

  it('deve renderizar o botão de sino', () => {
    useNotificationStore.setState({
      notifications: mockNotifications,
      unreadCount:   1,
    })
    renderDropdown()
    expect(document.querySelector('.lucide-bell')).toBeInTheDocument()
  })

  it('deve exibir badge com contagem de não lidas', () => {
    useNotificationStore.setState({
      notifications: mockNotifications,
      unreadCount:   1,
    })
    renderDropdown()
    expect(screen.getByText('1')).toBeInTheDocument()
  })

  it('deve abrir dropdown ao clicar no sino', async () => {
    useNotificationStore.setState({
      notifications: mockNotifications,
      unreadCount:   1,
    })
    renderDropdown()
    const user = userEvent.setup()

    await user.click(document.querySelector('.lucide-bell')!.closest('button')!)

    await waitFor(() => {
      expect(screen.getByText('Notificações')).toBeInTheDocument()
    })
  })

  it('deve exibir as notificações no dropdown', async () => {
    useNotificationStore.setState({
      notifications: mockNotifications,
      unreadCount:   1,
    })
    renderDropdown()
    const user = userEvent.setup()

    await user.click(document.querySelector('.lucide-bell')!.closest('button')!)

    await waitFor(() => {
      expect(screen.getByText('Orçamento de Alimentação atingiu 80%.')).toBeInTheDocument()
      expect(screen.getByText('Orçamento de Transporte atingiu 100%.')).toBeInTheDocument()
    })
  })

  it('deve exibir botão marcar todas quando há não lidas', async () => {
    useNotificationStore.setState({
      notifications: mockNotifications,
      unreadCount:   1,
    })
    renderDropdown()
    const user = userEvent.setup()

    await user.click(document.querySelector('.lucide-bell')!.closest('button')!)

    await waitFor(() => {
      expect(screen.getByText('Marcar todas')).toBeInTheDocument()
    })
  })

  it('deve chamar markAllAsRead ao clicar em marcar todas', async () => {
    useNotificationStore.setState({
      notifications: mockNotifications,
      unreadCount:   1,
    })
    renderDropdown()
    const user = userEvent.setup()

    await user.click(document.querySelector('.lucide-bell')!.closest('button')!)

    await waitFor(() => screen.getByText('Marcar todas'))
    await user.click(screen.getByText('Marcar todas'))

    expect(mockMarkAllAsRead).toHaveBeenCalledOnce()
  })

  it('deve exibir estado vazio quando não há notificações', async () => {
    renderDropdown()
    const user = userEvent.setup()

    await user.click(document.querySelector('.lucide-bell')!.closest('button')!)

    await waitFor(() => {
      expect(screen.getByText('Nenhuma notificação')).toBeInTheDocument()
    })
  })

  it('não deve exibir badge quando todas estão lidas', () => {
    renderDropdown()
    expect(screen.queryByText('1')).not.toBeInTheDocument()
  })

  // Testes Fase 9 — novos tipos de notificação

  it('deve exibir label correto para TransactionDueTomorrow', async () => {
    useNotificationStore.setState({
      notifications: mockDueNotifications,
      unreadCount:   2,
    })
    renderDropdown()
    const user = userEvent.setup()

    await user.click(document.querySelector('.lucide-bell')!.closest('button')!)

    await waitFor(() => {
      expect(screen.getByText('⏰ Vence amanhã')).toBeInTheDocument()
    })
  })

  it('deve exibir label correto para TransactionDueIn3Days', async () => {
    useNotificationStore.setState({
      notifications: mockDueNotifications,
      unreadCount:   2,
    })
    renderDropdown()
    const user = userEvent.setup()

    await user.click(document.querySelector('.lucide-bell')!.closest('button')!)

    await waitFor(() => {
      expect(screen.getByText('📅 Vence em 3 dias')).toBeInTheDocument()
    })
  })

  it('deve exibir mensagem de vencimento no dropdown', async () => {
    useNotificationStore.setState({
      notifications: mockDueNotifications,
      unreadCount:   2,
    })
    renderDropdown()
    const user = userEvent.setup()

    await user.click(document.querySelector('.lucide-bell')!.closest('button')!)

    await waitFor(() => {
      expect(screen.getByText(/Aluguel.*vence amanhã/i)).toBeInTheDocument()
      expect(screen.getByText(/Internet.*vence em 3 dias/i)).toBeInTheDocument()
    })
  })

  it('deve exibir badge com contagem correta para notificações de vencimento', () => {
    useNotificationStore.setState({
      notifications: mockDueNotifications,
      unreadCount:   2,
    })
    renderDropdown()
    expect(screen.getByText('2')).toBeInTheDocument()
  })
})