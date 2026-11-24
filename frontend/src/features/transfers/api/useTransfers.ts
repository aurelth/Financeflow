import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/apiError'
import type {
  CreateTransferRequest,
  LinkTransfersRequest,
  PendingTransfer,
  TransferLink,
  UnlinkTransferRequest,
} from '../types/transfer.types'

// Transferências mexem em saldos, lançamentos e relatórios
function invalidateTransferDependents(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: ['transfers'] })
  qc.invalidateQueries({ queryKey: ['transactions'] })
  qc.invalidateQueries({ queryKey: ['accounts'] })
  qc.invalidateQueries({ queryKey: ['dashboard'] })
}

// GET /api/transfers/pending — pontas sem par, com a melhor sugestão
export const usePendingTransfers = () =>
  useQuery({
    queryKey: ['transfers', 'pending'],
    queryFn:  () => api.get<PendingTransfer[]>('/api/transfers/pending').then(r => r.data),
    staleTime: 0,
  })

// POST /api/transfers — cria saída + entrada + vínculo de uma vez
export const useCreateTransfer = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateTransferRequest) =>
      api.post<TransferLink>('/api/transfers', data).then(r => r.data),
    onSuccess: () => {
      invalidateTransferDependents(qc)
      toast.success('Transferência registrada!')
    },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Erro ao registrar a transferência.')),
  })
}

// POST /api/transfers/link — une dois lançamentos existentes
export const useLinkTransfers = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: LinkTransfersRequest) =>
      api.post<TransferLink>('/api/transfers/link', data).then(r => r.data),
    onSuccess: () => {
      invalidateTransferDependents(qc)
      toast.success('Transferência vinculada!')
    },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Não foi possível vincular os lançamentos.')),
  })
}

// DELETE /api/transfers/{id} — devolve as pontas como despesa/receita ou exclui as duas
export const useUnlinkTransfer = (linkId: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UnlinkTransferRequest) =>
      api.delete(`/api/transfers/${linkId}`, { data }).then(r => r.data),
    onSuccess: () => {
      invalidateTransferDependents(qc)
      toast.success('Vínculo desfeito.')
    },
    onError: (err: unknown) => toast.error(getApiErrorMessage(err, 'Não foi possível desfazer o vínculo.')),
  })
}
