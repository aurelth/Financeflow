import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/apiError'
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
    staleTime: 0,
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

      if (data.accountId)
        formData.append('accountId', data.accountId)

      if (data.transferDirection)
        formData.append('transferDirection', data.transferDirection)

      data.tags.forEach(tag => formData.append('tags', tag))

      if (attachment)
        formData.append('attachment', attachment)

      return api.post<Transaction>('/api/transactions', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      }).then(r => r.data)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      // O saldo da conta e a conciliação com o extrato mudam a cada lançamento
      qc.invalidateQueries({ queryKey: ['accounts'] })
      toast.success('Transação criada com sucesso!')
    },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao criar transação. Tente novamente.')),
  })
}

export const useUpdateTransaction = (id: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateTransactionRequest) =>
      api.put<Transaction>(`/api/transactions/${id}`, data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      // O saldo da conta e a conciliação com o extrato mudam a cada lançamento
      qc.invalidateQueries({ queryKey: ['accounts'] })
      toast.success('Transação atualizada com sucesso!')
    },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao atualizar transação. Tente novamente.')),
  })
}

// Aceita objeto com id e deleteFuture
export const useDeleteTransaction = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: ({ id, deleteFuture }: { id: string; deleteFuture: boolean }) =>
      api.delete(`/api/transactions/${id}`, {
        params: { deleteFuture },
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      // O saldo da conta e a conciliação com o extrato mudam a cada lançamento
      qc.invalidateQueries({ queryKey: ['accounts'] })
      toast.success('Transação removida com sucesso!')
    },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao remover transação. Tente novamente.')),
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
      qc.invalidateQueries({ queryKey: ['transactions'] })
      toast.success('Anexo enviado com sucesso!')
    },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao enviar anexo. Tente novamente.')),
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
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao remover comprovante. Tente novamente.')),
  })
}

export const getAttachmentUrl = (transactionId: string): string =>
  `${import.meta.env.VITE_API_URL}/api/transactions/${transactionId}/attachment`