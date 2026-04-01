import { useEffect } from 'react'
import * as signalR from '@microsoft/signalr'
import { toast } from 'sonner'
import { useNotificationStore } from '@/store/notificationStore'
import type { Notification } from '@/features/notifications/types/notification.types'

export function useNotificationHub() {
  const addNotification = useNotificationStore(s => s.addNotification)

  useEffect(() => {
    const token = sessionStorage.getItem('accessToken')
    if (!token) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7195/hubs/notifications', {
        transport:   signalR.HttpTransportType.LongPolling,
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build()

    connection.on('ReceiveNotification', (data: { type: string; message: string }) => {
      const notification: Notification = {
        id:        crypto.randomUUID(),
        type:      data.type,
        message:   data.message,
        isRead:    false,
        createdAt: new Date().toISOString(),
      }

      addNotification(notification)

      // Toast diferenciado por tipo
      if (data.type === 'BudgetCritical') {
        toast.error(data.message, { duration: 6000 })
      } else {
        toast.warning(data.message, { duration: 5000 })
      }
    })

    connection
      .start()
      .catch(err =>
        console.error('Erro ao conectar ao NotificationHub:', err)
      )

    return () => {
      connection.stop()
    }
  }, [addNotification])
}