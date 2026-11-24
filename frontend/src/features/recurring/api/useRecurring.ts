import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/apiError'
import type {
  ConfirmRecurringResult,
  RecurringSuggestionRef,
  RecurringSuggestions,
} from '../types/recurring.types'

export const RECURRING_SUGGESTIONS_KEY = ['recurring', 'suggestions'] as const

// A detecção é recalculada pelo servidor a partir do histórico; cinco minutos evitam refazê-la a cada navegação
export const useRecurringSuggestions = () =>
  useQuery({
    queryKey:  RECURRING_SUGGESTIONS_KEY,
    queryFn:   () => api.get<RecurringSuggestions>('/api/recurring/suggestions').then(r => r.data),
    staleTime: 5 * 60 * 1000,
  })

// Confirmar liga o histórico a um grupo e agenda as próximas: lista, dashboard, saldos e orçamentos mudam
export const useConfirmRecurring = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (ref: RecurringSuggestionRef) =>
      api.post<ConfirmRecurringResult>('/api/recurring/suggestions/confirm', ref).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: RECURRING_SUGGESTIONS_KEY })
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      qc.invalidateQueries({ queryKey: ['budgets'] })
    },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao confirmar a recorrência. Tente novamente.')),
  })
}

export const useDismissRecurring = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (ref: RecurringSuggestionRef) =>
      api.post('/api/recurring/suggestions/dismiss', ref),
    onSuccess: () => { qc.invalidateQueries({ queryKey: RECURRING_SUGGESTIONS_KEY }) },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao ignorar a sugestão. Tente novamente.')),
  })
}

export const useResetDismissedRecurring = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: () =>
      api.delete<{ restored: number }>('/api/recurring/suggestions/dismissed').then(r => r.data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: RECURRING_SUGGESTIONS_KEY }) },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao restaurar as sugestões. Tente novamente.')),
  })
}
