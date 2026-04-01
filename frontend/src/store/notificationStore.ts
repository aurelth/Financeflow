import { create } from 'zustand'
import type { Notification } from '@/features/notifications/types/notification.types'

interface NotificationState {
  notifications:  Notification[]
  unreadCount:    number
  setNotifications: (notifications: Notification[]) => void
  addNotification:  (notification: Notification)    => void
  markAsRead:       (id: string)                    => void
  markAllAsRead:    ()                              => void
}

export const useNotificationStore = create<NotificationState>((set, get) => ({
  notifications: [],
  unreadCount:   0,

  setNotifications: (notifications) =>
    set({
      notifications,
      unreadCount: notifications.filter(n => !n.isRead).length,
    }),

  addNotification: (notification) => {
    const notifications = [notification, ...get().notifications]
    set({
      notifications,
      unreadCount: notifications.filter(n => !n.isRead).length,
    })
  },

  markAsRead: (id) => {
    const notifications = get().notifications.map(n =>
      n.id === id ? { ...n, isRead: true } : n
    )
    set({
      notifications,
      unreadCount: notifications.filter(n => !n.isRead).length,
    })
  },

  markAllAsRead: () => {
    const notifications = get().notifications.map(n => ({ ...n, isRead: true }))
    set({ notifications, unreadCount: 0 })
  },
}))