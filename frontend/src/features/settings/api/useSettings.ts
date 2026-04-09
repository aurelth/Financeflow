import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import { useAuthStore } from '@/store/authStore'
import type {
  NotificationPreferences,
  UpdateNotificationPreferencesRequest,
  DeleteAccountRequest,
} from '../types/settings.types'

// GET /api/settings/notifications
export function useNotificationPreferences() {
  return useQuery({
    queryKey: ['settings', 'notifications'],
    queryFn: async () => {
      const { data } = await api.get<NotificationPreferences>(
        '/api/settings/notifications'
      )
      return data
    },
  })
}

// PUT /api/settings/notifications
export function useUpdateNotificationPreferences() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (body: UpdateNotificationPreferencesRequest) => {
      await api.put('/api/settings/notifications', body)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings', 'notifications'] })
      toast.success('Preferências de notificação atualizadas.')
    },
    onError: () => {
      toast.error('Erro ao atualizar preferências.')
    },
  })
}

// POST /api/settings/logout-all
export function useLogoutAll() {
  const logout = useAuthStore(s => s.logout)

  return useMutation({
    mutationFn: async () => {
      await api.post('/api/settings/logout-all')
    },
    onSuccess: () => {
      toast.success('Todas as sessões foram encerradas.')
      logout()
    },
    onError: () => {
      toast.error('Erro ao encerrar sessões.')
    },
  })
}

// DELETE /api/settings/account
export function useDeleteAccount() {
  const logout = useAuthStore(s => s.logout)

  return useMutation({
    mutationFn: async (body: DeleteAccountRequest) => {
      await api.delete('/api/settings/account', { data: body })
    },
    onSuccess: () => {
      toast.success('Conta excluída com sucesso.')
      logout()
    },
    onError: () => {
      toast.error('Senha incorreta ou erro ao excluir conta.')
    },
  })
}