import { Bell, Check, CheckCheck } from 'lucide-react'
import { useEffect } from 'react'
import { formatDistanceToNow } from 'date-fns'
import { ptBR } from 'date-fns/locale'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useNotificationStore } from '@/store/notificationStore'
import { useNotifications, useMarkAsRead, useMarkAllAsRead } from '../api/useNotifications'
import type { Notification } from '../types/notification.types'

export default function NotificationDropdown() {
  const { data, isLoading }       = useNotifications()
  const { mutate: markAsRead }    = useMarkAsRead()
  const { mutate: markAllAsRead } = useMarkAllAsRead()
  const {
    notifications, unreadCount,
    setNotifications, markAsRead: storeMarkAsRead, markAllAsRead: storeMarkAllAsRead,
  } = useNotificationStore()

  useEffect(() => {
    if (data) setNotifications(data)
  }, [data, setNotifications])

  const handleMarkAsRead    = (id: string) => { markAsRead(id); storeMarkAsRead(id) }
  const handleMarkAllAsRead = () => { markAllAsRead(); storeMarkAllAsRead() }

  // Config de tipos com tokens da nova paleta
  const typeConfig: Record<string, { bg: string; border: string; color: string; label: string }> = {
    BudgetCritical:         { bg: 'rgba(244,63,94,0.08)',  border: 'rgba(244,63,94,0.2)',  color: 'var(--ff-expense)',   label: '🚨 Crítico'        },
    BudgetWarning:          { bg: 'rgba(245,158,11,0.08)', border: 'rgba(245,158,11,0.2)', color: 'var(--ff-pending)',   label: '🔔 Aviso'          },
    TransactionDueTomorrow: { bg: 'rgba(245,158,11,0.08)', border: 'rgba(245,158,11,0.2)', color: 'var(--ff-pending)',   label: '⏰ Vence amanhã'   },
    TransactionDueIn3Days:  { bg: 'rgba(99,102,241,0.08)', border: 'rgba(99,102,241,0.2)', color: 'var(--ff-scheduled)', label: '📅 Vence em 3 dias' },
  }

  const defaultType = { bg: 'var(--ff-bg-elevated)', border: 'var(--ff-border)', color: 'var(--ff-text-muted)', label: '🔔 Notificação' }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          className="relative p-2 rounded-xl transition-colors"
          style={{ color: 'var(--ff-text-muted)' }}
          onMouseEnter={e => {
            e.currentTarget.style.color = 'var(--ff-text-primary)'
            e.currentTarget.style.background = 'var(--ff-bg-elevated)'
          }}
          onMouseLeave={e => {
            e.currentTarget.style.color = 'var(--ff-text-muted)'
            e.currentTarget.style.background = 'transparent'
          }}
        >
          <Bell size={18} />
          {unreadCount > 0 && (
            <span
              className="absolute -top-1 -right-1 w-5 h-5 text-[10px] font-bold rounded-full flex items-center justify-center animate-pulse"
              style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
            >
              {unreadCount > 9 ? '9+' : unreadCount}
            </span>
          )}
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent
        align="end"
        className="w-96 p-0"
        style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      >
        {/* Header */}
        <div
          className="flex items-center justify-between px-4 py-3"
          style={{ borderBottom: '1px solid var(--ff-border)' }}
        >
          <div className="flex items-center gap-2">
            <Bell size={15} style={{ color: 'var(--ff-text-muted)' }} />
            <span className="text-sm font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
              Notificações
            </span>
            {unreadCount > 0 && (
              <span
                className="text-xs px-2 py-0.5 rounded-full font-medium"
                style={{ background: 'var(--ff-emerald-subtle)', color: 'var(--ff-emerald)' }}
              >
                {unreadCount} nova{unreadCount > 1 ? 's' : ''}
              </span>
            )}
          </div>
          {unreadCount > 0 && (
            <button
              onClick={handleMarkAllAsRead}
              className="flex items-center gap-1 text-xs transition-colors"
              style={{ color: 'var(--ff-emerald)' }}
              onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald-hover)')}
              onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
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
              <div
                className="w-5 h-5 border-2 border-t-transparent rounded-full animate-spin"
                style={{ borderColor: 'var(--ff-emerald)', borderTopColor: 'transparent' }}
              />
            </div>
          ) : notifications.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-10 gap-2">
              <Bell size={28} style={{ color: 'var(--ff-text-muted)' }} />
              <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
                Nenhuma notificação
              </p>
            </div>
          ) : (
            notifications.map(n => (
              <NotificationItem
                key={n.id}
                notification={n}
                config={typeConfig[n.type] ?? defaultType}
                onMarkAsRead={handleMarkAsRead}
              />
            ))
          )}
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

interface NotificationItemProps {
  notification: Notification
  config:       { bg: string; border: string; color: string; label: string }
  onMarkAsRead: (id: string) => void
}

function NotificationItem({ notification, config, onMarkAsRead }: NotificationItemProps) {
  const dateStr = notification.createdAt.endsWith('Z')
    ? notification.createdAt
    : `${notification.createdAt}Z`

  const timeAgo = formatDistanceToNow(new Date(dateStr), { addSuffix: true, locale: ptBR })

  return (
    <div
      className="px-4 py-3 transition-colors"
      style={{
        borderBottom: '1px solid var(--ff-border-subtle)',
        opacity:      notification.isRead ? 0.6 : 1,
        background:   notification.isRead ? 'transparent' : 'var(--ff-bg-elevated)',
      }}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <span
            className="inline-block text-[10px] font-semibold px-2 py-0.5 rounded-full mb-1.5"
            style={{ background: config.bg, border: `1px solid ${config.border}`, color: config.color }}
          >
            {config.label}
          </span>
          <p className="text-xs leading-relaxed" style={{ color: 'var(--ff-text-secondary)' }}>
            {notification.message}
          </p>
          <p className="text-[11px] mt-1" style={{ color: 'var(--ff-text-muted)' }}>
            {timeAgo}
          </p>
        </div>
        {!notification.isRead && (
          <button
            onClick={() => onMarkAsRead(notification.id)}
            title="Marcar como lida"
            className="mt-1 flex-shrink-0 transition-colors"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
            onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
          >
            <Check size={14} />
          </button>
        )}
      </div>
    </div>
  )
}