import { useQuery } from '@tanstack/react-query'
import api from '@/lib/axios'
import type { HealthScoreResult, HealthScoreHistoryItem } from '../types/healthscore.types'

export const useHealthScore = (month?: number, year?: number) =>
  useQuery({
    queryKey: ['healthscore', month, year],
    queryFn:  () =>
      api.get<HealthScoreResult>('/api/healthscore', {
        params: { month, year },
      }).then(r => r.data),
    staleTime: 0,
  })

export const useHealthScoreHistory = () =>
  useQuery({
    queryKey: ['healthscore', 'history'],
    queryFn:  () =>
      api.get<HealthScoreHistoryItem[]>('/api/healthscore/history')
        .then(r => r.data),
    staleTime: 0,
  })