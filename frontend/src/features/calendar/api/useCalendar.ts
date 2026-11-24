import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/apiError'
import type { Transaction } from '../../transactions/types/transaction.types'
import type { CalendarMonth } from '../types/calendar.types'

export const CALENDAR_KEY = ['calendar'] as const

export const useCalendarMonth = (year: number, month: number) =>
  useQuery({
    queryKey: [...CALENDAR_KEY, year, month],
    queryFn:  () =>
      api.get<CalendarMonth>('/api/calendar', { params: { month, year } }).then(r => r.data),
    // Dar baixa muda o calendário a qualquer momento (e em outra aba): sempre confere ao voltar
    staleTime: 0,
  })

// Dar baixa vira o lançamento em Pago: ele passa a contar no saldo, no dashboard e nos relatórios
export const usePayTransaction = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.post<Transaction>(`/api/transactions/${id}/pay`).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: CALENDAR_KEY })
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      qc.invalidateQueries({ queryKey: ['accounts'] })
      qc.invalidateQueries({ queryKey: ['budgets'] })
    },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao dar baixa. Tente novamente.')),
  })
}
