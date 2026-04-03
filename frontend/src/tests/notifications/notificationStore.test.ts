import { describe, it, expect, beforeEach } from 'vitest'
import { useNotificationStore } from '@/store/notificationStore'
import type { Notification } from '@/features/notifications/types/notification.types'

const mockNotification: Notification = {
  id:        'notif-1',
  type:      'BudgetWarning',
  message:   'Orçamento atingiu 80%.',
  isRead:    false,
  referenceId: null,
  createdAt: '2026-03-15T10:00:00Z',
}

const mockNotification2: Notification = {
  id:        'notif-2',
  type:      'BudgetCritical',
  message:   'Orçamento atingiu 100%.',
  isRead:    false,
  referenceId: null,
  createdAt: '2026-03-15T11:00:00Z',
}

describe('notificationStore', () => {
  beforeEach(() => {
    useNotificationStore.setState({
      notifications: [],
      unreadCount:   0,
    })
  })

  it('deve inicializar com estado vazio', () => {
    const { notifications, unreadCount } = useNotificationStore.getState()
    expect(notifications).toHaveLength(0)
    expect(unreadCount).toBe(0)
  })

  it('deve definir notificações e calcular unreadCount', () => {
    useNotificationStore.getState().setNotifications([
      mockNotification,
      mockNotification2,
    ])

    const { notifications, unreadCount } = useNotificationStore.getState()
    expect(notifications).toHaveLength(2)
    expect(unreadCount).toBe(2)
  })

  it('deve adicionar notificação no início da lista', () => {
    useNotificationStore.getState().setNotifications([mockNotification])
    useNotificationStore.getState().addNotification(mockNotification2)

    const { notifications } = useNotificationStore.getState()
    expect(notifications[0].id).toBe('notif-2')
    expect(notifications).toHaveLength(2)
  })

  it('deve incrementar unreadCount ao adicionar notificação', () => {
    useNotificationStore.getState().addNotification(mockNotification)
    expect(useNotificationStore.getState().unreadCount).toBe(1)

    useNotificationStore.getState().addNotification(mockNotification2)
    expect(useNotificationStore.getState().unreadCount).toBe(2)
  })

  it('deve marcar notificação individual como lida', () => {
    useNotificationStore.getState().setNotifications([
      mockNotification,
      mockNotification2,
    ])

    useNotificationStore.getState().markAsRead('notif-1')

    const { notifications, unreadCount } = useNotificationStore.getState()
    expect(notifications.find(n => n.id === 'notif-1')?.isRead).toBe(true)
    expect(notifications.find(n => n.id === 'notif-2')?.isRead).toBe(false)
    expect(unreadCount).toBe(1)
  })

  it('deve marcar todas as notificações como lidas', () => {
    useNotificationStore.getState().setNotifications([
      mockNotification,
      mockNotification2,
    ])

    useNotificationStore.getState().markAllAsRead()

    const { notifications, unreadCount } = useNotificationStore.getState()
    expect(notifications.every(n => n.isRead)).toBe(true)
    expect(unreadCount).toBe(0)
  })

  it('não deve alterar unreadCount ao marcar notificação já lida', () => {
    useNotificationStore.getState().setNotifications([
      { ...mockNotification, isRead: true },
    ])

    useNotificationStore.getState().markAsRead('notif-1')

    expect(useNotificationStore.getState().unreadCount).toBe(0)
  })
})