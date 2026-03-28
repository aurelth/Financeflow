import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import type {
  Transaction,
  PagedResult,
  CreateTransactionRequest,
  UpdateTransactionRequest,
  GetTransactionsQuery,
} from '../types/transaction.types'

// Queries

export const useTransactions = (filters: GetTransactionsQuery = {}) =>
  useQuery({
    queryKey: ['transactions', filters],
    queryFn:  () =>
      api.get<PagedResult<Transaction>>('/api/transactions', {
        params: filters,
      }).then(r => r.data),
    staleTime: 1 * 60 * 1000,
  })

export const useTransaction = (id: string) =>
  useQuery({
    queryKey: ['transactions', id],
    queryFn:  () =>
      api.get<Transaction>(`/api/transactions/${id}`).then(r => r.data),
    enabled:  !!id,
    staleTime: 1 * 60 * 1000,
  })

// Mutations

export const useCreateTransaction = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: ({ data, attachment }: {
      data:       CreateTransactionRequest
      attachment?: File
    }) => {
      const formData = new FormData()

      // Campos escalares
      formData.append('amount',         String(data.amount))
      formData.append('type',           String(data.type))
      formData.append('date',           data.date)
      formData.append('description',    data.description)
      formData.append('status',         String(data.status))
      formData.append('isRecurring',    String(data.isRecurring))
      formData.append('recurrenceType', String(data.recurrenceType))
      formData.append('categoryId',     data.categoryId)

      if (data.subcategoryId)
        formData.append('subcategoryId', data.subcategoryId)

      // Tags como array
      data.tags.forEach(tag => formData.append('tags', tag))

      // Anexo opcional
      if (attachment)
        formData.append('attachment', attachment)

      return api.post<Transaction>('/api/transactions', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      }).then(r => r.data)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      toast.success('Transação criada com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error(
          err.response?.data?.message ?? 'Erro ao criar transação. Tente novamente.'
        )
      }
    },
  })
}

export const useUpdateTransaction = (id: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateTransactionRequest) =>
      api.put<Transaction>(`/api/transactions/${id}`, data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      toast.success('Transação atualizada com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error(
          err.response?.data?.message ?? 'Erro ao atualizar transação. Tente novamente.'
        )
      }
    },
  })
}

export const useDeleteTransaction = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.delete(`/api/transactions/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      toast.success('Transação removida com sucesso!')
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message
      toast.error(msg ?? 'Erro ao remover transação. Tente novamente.')
    },
  })
}

export const useUploadAttachment = (id: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      return api.post<Transaction>(
        `/api/transactions/${id}/attachment`,
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } }
      ).then(r => r.data)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions', id] })
      toast.success('Anexo enviado com sucesso!')
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message
      toast.error(msg ?? 'Erro ao enviar anexo. Tente novamente.')
    },
  })
}

export const useRemoveAttachment = (id: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: () =>
      api.delete(`/api/transactions/${id}/attachment`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      toast.success('Comprovante removido com sucesso!')
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message
      toast.error(msg ?? 'Erro ao remover comprovante. Tente novamente.')
    },
  })
}