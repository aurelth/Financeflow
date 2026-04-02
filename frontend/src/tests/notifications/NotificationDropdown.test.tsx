import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import NotificationDropdown from '@/features/notifications/components/NotificationDropdown'
import { useNotificationStore } from '@/store/notificationStore'
import type { Notification } from '@/features/notifications/types/notification.types'

const mockNotifications: Notification[] = [
  {
    id:        'notif-1',
    type:      'BudgetWarning',
    message:   'Orçamento de Alimentação atingiu 80%.',
    isRead:    false,
    createdAt: '2026-03-15T10:00:00Z',
  },
  {
    id:        'notif-2',
    type:      'BudgetCritical',
    message:   'Orçamento de Transporte atingiu 100%.',
    isRead:    true,
    createdAt: '2026-03-14T09:00:00Z',
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
    // Reset completo antes de cada teste
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
    // store já está vazio pelo beforeEach
    renderDropdown()
    const user = userEvent.setup()

    await user.click(document.querySelector('.lucide-bell')!.closest('button')!)

    await waitFor(() => {
      expect(screen.getByText('Nenhuma notificação')).toBeInTheDocument()
    })
  })

  it('não deve exibir badge quando todas estão lidas', () => {
    // store já está vazio pelo beforeEach — sem badge
    renderDropdown()
    expect(screen.queryByText('1')).not.toBeInTheDocument()
  })
})