/* eslint-disable @typescript-eslint/no-explicit-any */
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import type {
  GoalsSummaryResultDto,
  GoalProgressResultDto,
  CreateGoalRequest,
  UpdateGoalRequest,
} from '../types/goal.types'
import type { PagedResult, Transaction } from '@/features/transactions/types/transaction.types'

export const useGoals = () =>
  useQuery({
    queryKey: ['goals'],
    queryFn:  () =>
      api.get<GoalsSummaryResultDto>('/api/goals').then(r => r.data),
    staleTime: 0,
  })

export const useGoalContributions = (categoryId: string | null) =>
  useQuery({
    queryKey: ['goal-contributions', categoryId],
    queryFn:  () =>
      api.get<PagedResult<Transaction>>(
        `/api/transactions?categoryId=${categoryId}&pageSize=50&page=1`
      ).then(r => r.data),
    enabled:   !!categoryId,
    staleTime: 0,
  })

export const useCreateGoal = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateGoalRequest) =>
      api.post<GoalProgressResultDto>('/api/goals', data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['goals'] })
      qc.invalidateQueries({ queryKey: ['categories'] })
      qc.invalidateQueries({ queryKey: ['categories', 'goals'] })
      toast.success('Meta criada com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error(err.response?.data?.message ?? 'Erro ao criar meta. Tente novamente.')
      }
    },
  })
}

export const useUpdateGoal = (id: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateGoalRequest) =>
      api.put<GoalProgressResultDto>(`/api/goals/${id}`, data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['goals'] })
      qc.invalidateQueries({ queryKey: ['categories'] })
      qc.invalidateQueries({ queryKey: ['categories', 'goals'] })
      toast.success('Meta atualizada com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error(err.response?.data?.message ?? 'Erro ao atualizar meta. Tente novamente.')
      }
    },
  })
}

export const useDeleteGoal = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.delete(`/api/goals/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['goals'] })
      qc.invalidateQueries({ queryKey: ['categories'] })
      qc.invalidateQueries({ queryKey: ['categories', 'goals'] })
      toast.success('Meta removida com sucesso!')
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message
      toast.error(msg ?? 'Erro ao remover meta. Tente novamente.')
    },
  })
}