import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import type { Notification } from '../types/notification.types'

// Queries

export const useNotifications = () =>
  useQuery({
    queryKey: ['notifications'],
    queryFn:  () =>
      api.get<Notification[]>('/api/notifications').then(r => r.data),
    staleTime: 0,
  })

// Mutations

export const useMarkAsRead = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.patch(`/api/notifications/${id}/read`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['notifications'] })
    },
    onError: () => {
      toast.error('Erro ao marcar notificação como lida.')
    },
  })
}

export const useMarkAllAsRead = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: () =>
      api.patch('/api/notifications/read-all'),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['notifications'] })
    },
    onError: () => {
      toast.error('Erro ao marcar todas as notificações como lidas.')
    },
  })
}