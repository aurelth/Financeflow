/* eslint-disable @typescript-eslint/no-explicit-any */
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import type {
  Account,
  AccountReconciliation,
  CreateAccountRequest,
  UpdateAccountRequest,
} from '../types/account.types'

// Lista as contas do utilizador. Com includeArchived=false (padrão) só vêm as ativas.
export const useAccounts = (includeArchived = false) =>
  useQuery({
    queryKey: ['accounts', { includeArchived }],
    queryFn:  () =>
      api.get<Account[]>('/api/accounts', { params: { includeArchived } })
        .then(r => r.data),
    staleTime: 60 * 1000,
  })

// Conciliação de todas as contas ativas com o último extrato confirmado de cada uma.
// A chave começa com 'accounts', então qualquer invalidação de contas também a atualiza.
export const useAccountsReconciliation = () =>
  useQuery({
    queryKey: ['accounts', 'reconciliation'],
    queryFn:  () =>
      api.get<AccountReconciliation[]>('/api/accounts/reconciliation').then(r => r.data),
    staleTime: 60 * 1000,
  })

function extractError(err: any, fallback: string): string {
  const errors = err.response?.data?.errors
  if (errors) return Object.values(errors).flat().join(' ')
  return err.response?.data?.message ?? fallback
}

// Lançamentos e dashboard dependem do saldo das contas, por isso também são invalidados
function invalidateAccountDependents(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: ['accounts'] })
  qc.invalidateQueries({ queryKey: ['transactions'] })
  qc.invalidateQueries({ queryKey: ['dashboard'] })
}

export const useCreateAccount = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateAccountRequest) =>
      api.post<Account>('/api/accounts', data).then(r => r.data),
    onSuccess: () => invalidateAccountDependents(qc),
    onError: (err: any) => toast.error(extractError(err, 'Erro ao criar conta.')),
  })
}

export const useUpdateAccount = (id: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateAccountRequest) =>
      api.put<Account>(`/api/accounts/${id}`, data).then(r => r.data),
    onSuccess: () => invalidateAccountDependents(qc),
    onError: (err: any) => toast.error(extractError(err, 'Erro ao atualizar conta.')),
  })
}

// DELETE arquiva a conta (preserva o histórico)
export const useArchiveAccount = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.delete(`/api/accounts/${id}`),
    onSuccess: () => invalidateAccountDependents(qc),
    onError: (err: any) => toast.error(extractError(err, 'Erro ao arquivar conta.')),
  })
}

// Reativa uma conta arquivada reaproveitando o PUT com isArchived=false
export const useRestoreAccount = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (account: Account) =>
      api.put<Account>(`/api/accounts/${account.id}`, {
        name:               account.name,
        type:               account.type,
        bankName:           account.bankName,
        initialBalance:     account.initialBalance,
        initialBalanceDate: account.initialBalanceDate,
        currency:           account.currency,
        color:              account.color,
        icon:               account.icon,
        isArchived:         false,
      } satisfies UpdateAccountRequest).then(r => r.data),
    onSuccess: () => invalidateAccountDependents(qc),
    onError: (err: any) => toast.error(extractError(err, 'Erro ao reativar conta.')),
  })
}
