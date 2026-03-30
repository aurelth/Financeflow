import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import type {
  Budget,
  BudgetSummary,
  CreateBudgetRequest,
  UpdateBudgetRequest,
  GetBudgetsQuery,
} from '../types/budget.types'

// Queries

export const useBudgets = (filters: GetBudgetsQuery) =>
  useQuery({
    queryKey: ['budgets', filters],
    queryFn:  () =>
      api.get<Budget[]>('/api/budgets', {
        params: filters,
      }).then(r => r.data),
    staleTime: 0,
  })

export const useBudgetSummary = (filters: GetBudgetsQuery) =>
  useQuery({
    queryKey: ['budgets', 'summary', filters],
    queryFn:  () =>
      api.get<BudgetSummary[]>('/api/budgets/summary', {
        params: filters,
      }).then(r => r.data),
    staleTime: 0,
  })

// Mutations

export const useCreateBudget = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateBudgetRequest) =>
      api.post<Budget>('/api/budgets', data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['budgets'] })
      toast.success('Orçamento criado com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error(
          err.response?.data?.message ?? 'Erro ao criar orçamento. Tente novamente.'
        )
      }
    },
  })
}

export const useUpdateBudget = (id: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateBudgetRequest) =>
      api.put<Budget>(`/api/budgets/${id}`, data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['budgets'] })
      toast.success('Orçamento atualizado com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error(
          err.response?.data?.message ?? 'Erro ao atualizar orçamento. Tente novamente.'
        )
      }
    },
  })
}

export const useDeleteBudget = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.delete(`/api/budgets/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['budgets'] })
      toast.success('Orçamento removido com sucesso!')
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message
      toast.error(msg ?? 'Erro ao remover orçamento. Tente novamente.')
    },
  })
}