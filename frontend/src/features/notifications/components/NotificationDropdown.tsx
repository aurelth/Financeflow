import { Bell, Check, CheckCheck } from 'lucide-react'
import { useEffect } from 'react'
import { formatDistanceToNow } from 'date-fns'
import { ptBR } from 'date-fns/locale'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Button } from '@/components/ui/button'
import { useNotificationStore } from '@/store/notificationStore'
import { useNotifications, useMarkAsRead, useMarkAllAsRead } from '../api/useNotifications'
import type { Notification } from '../types/notification.types'

export default function NotificationDropdown() {
  const { data, isLoading }             = useNotifications()
  const { mutate: markAsRead }          = useMarkAsRead()
  const { mutate: markAllAsRead }       = useMarkAllAsRead()
  const { notifications, unreadCount, setNotifications, markAsRead: storeMarkAsRead, markAllAsRead: storeMarkAllAsRead } =
    useNotificationStore()

  // Sincroniza o store com os dados da API ao carregar
  useEffect(() => {
    if (data) setNotifications(data)
  }, [data, setNotifications])

  const handleMarkAsRead = (id: string) => {
    markAsRead(id)
    storeMarkAsRead(id)
  }

  const handleMarkAllAsRead = () => {
    markAllAsRead()
    storeMarkAllAsRead()
  }

  const getTypeStyles = (type: string) =>
    type === 'BudgetCritical'
      ? 'bg-red-500/10 border-red-500/20 text-red-400'
      : 'bg-yellow-500/10 border-yellow-500/20 text-yellow-400'

  const getTypeLabel = (type: string) =>
    type === 'BudgetCritical' ? '🚨 Crítico' : '🔔 Aviso'

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="relative text-slate-400 hover:text-slate-200 hover:bg-slate-800"
        >
          <Bell size={18} />
          {unreadCount > 0 && (
            <span className="absolute -top-1 -right-1 w-5 h-5 bg-indigo-500 text-white text-[10px] font-bold rounded-full flex items-center justify-center animate-pulse">
              {unreadCount > 9 ? '9+' : unreadCount}
            </span>
          )}
        </Button>
      </DropdownMenuTrigger>

      <DropdownMenuContent
        align="end"
        className="w-96 bg-slate-900 border-slate-800 p-0"
      >
        {/* Header */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-slate-800">
          <div className="flex items-center gap-2">
            <Bell size={15} className="text-slate-400" />
            <span className="text-slate-200 text-sm font-semibold">Notificações</span>
            {unreadCount > 0 && (
              <span className="bg-indigo-500/20 text-indigo-400 text-xs px-2 py-0.5 rounded-full font-medium">
                {unreadCount} nova{unreadCount > 1 ? 's' : ''}
              </span>
            )}
          </div>
          {unreadCount > 0 && (
            <button
              onClick={handleMarkAllAsRead}
              className="flex items-center gap-1 text-xs text-indigo-400 hover:text-indigo-300 transition-colors"
            >
              <CheckCheck size={13} />
              Marcar todas
            </button>
          )}
        </div>

        {/* Lista */}
        <div className="max-h-80 overflow-y-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <div className="w-5 h-5 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin" />
            </div>
          ) : notifications.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-10 gap-2">
              <Bell size={28} className="text-slate-600" />
              <p className="text-slate-500 text-sm">Nenhuma notificação</p>
            </div>
          ) : (
            notifications.map(n => (
              <NotificationItem
                key={n.id}
                notification={n}
                onMarkAsRead={handleMarkAsRead}
                getTypeStyles={getTypeStyles}
                getTypeLabel={getTypeLabel}
              />
            ))
          )}
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

interface NotificationItemProps {
  notification:   Notification
  onMarkAsRead:   (id: string) => void
  getTypeStyles:  (type: string) => string
  getTypeLabel:   (type: string) => string
}

function NotificationItem({
  notification,
  onMarkAsRead,
  getTypeStyles,
  getTypeLabel,
}: NotificationItemProps) {
  const dateStr    = notification.createdAt.endsWith('Z')
    ? notification.createdAt
    : `${notification.createdAt}Z`

  const timeAgo = formatDistanceToNow(new Date(dateStr), {
    addSuffix: true,
    locale:    ptBR,
  })

  return (
    <div
      className={`px-4 py-3 border-b border-slate-800/50 transition-colors ${
        notification.isRead ? 'opacity-60' : 'bg-slate-800/30'
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <span className={`inline-block text-[10px] font-semibold px-2 py-0.5 rounded-full border mb-1.5 ${getTypeStyles(notification.type)}`}>
            {getTypeLabel(notification.type)}
          </span>
          <p className="text-slate-300 text-xs leading-relaxed">
            {notification.message}
          </p>
          <p className="text-slate-500 text-[11px] mt-1">{timeAgo}</p>
        </div>
        {!notification.isRead && (
          <button
            onClick={() => onMarkAsRead(notification.id)}
            title="Marcar como lida"
            className="mt-1 text-slate-500 hover:text-indigo-400 transition-colors flex-shrink-0"
          >
            <Check size={14} />
          </button>
        )}
      </div>
    </div>
  )
}