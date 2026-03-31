import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import type { Report, CreateReportRequest } from '../types/report.types'

// Queries

export const useReports = () =>
  useQuery({
    queryKey: ['reports'],
    queryFn:  () =>
      api.get<Report[]>('/api/reports').then(r => r.data),
    staleTime: 0,
  })

// Mutations

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
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error(
          err.response?.data?.message ?? 'Erro ao solicitar relatório. Tente novamente.'
        )
      }
    },
  })
}

// URL de download

export const getReportDownloadUrl = (reportId: string): string =>
  `${import.meta.env.VITE_API_URL}/api/reports/${reportId}/download`