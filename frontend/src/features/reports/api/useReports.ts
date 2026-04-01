import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import type { Report, CreateReportRequest } from '../types/report.types'

export const useReports = () =>
  useQuery({
    queryKey: ['reports'],
    queryFn: () =>
      api.get<Report[]>('/api/reports').then(r => r.data),
    staleTime: 0,
  })

export const useRequestReport = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateReportRequest) =>
      api.post<Report>('/api/reports/request', data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['reports'] })
      toast.success('Relatório solicitado! Você será notificado quando estiver pronto.')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      const message = err.response?.data?.message

      const hasErrors = errors && Object.keys(errors).length > 0

      if (hasErrors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else if (message) {
        toast.error(message)
      } else {
        toast.error('Erro ao solicitar relatório. Tente novamente.')
      }
    },
  })
}

export const useDeleteReport = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.delete(`/api/reports/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['reports'] })
      toast.success('Relatório removido com sucesso.')
    },
    onError: () => {
      toast.error('Erro ao remover o relatório.')
    },
  })
}

export const getReportDownloadUrl = (reportId: string): string =>
  `${import.meta.env.VITE_API_URL}/api/reports/${reportId}/download`