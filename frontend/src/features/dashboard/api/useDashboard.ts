import { useQuery } from '@tanstack/react-query'
import api from '@/lib/axios'
import type {
  DashboardSummary,
  BalanceEvolution,
  ExpensesByCategory,
  WeeklyComparison,
  DashboardQuery,
} from '../types/dashboard.types'

export const useDashboardSummary = (filters: DashboardQuery) =>
  useQuery({
    queryKey: ['dashboard', 'summary', filters],
    queryFn:  () =>
      api.get<DashboardSummary>('/api/dashboard/summary', {
        params: filters,
      }).then(r => r.data),
    staleTime: 0,
  })

export const useBalanceEvolution = (filters: DashboardQuery) =>
  useQuery({
    queryKey: ['dashboard', 'balance-evolution', filters],
    queryFn:  () =>
      api.get<BalanceEvolution[]>('/api/dashboard/balance-evolution', {
        params: filters,
      }).then(r => r.data),
    staleTime: 0,
  })

export const useExpensesByCategory = (filters: DashboardQuery) =>
  useQuery({
    queryKey: ['dashboard', 'expenses-by-category', filters],
    queryFn:  () =>
      api.get<ExpensesByCategory[]>('/api/dashboard/expenses-by-category', {
        params: filters,
      }).then(r => r.data),
    staleTime: 0,
  })

export const useWeeklyComparison = (filters: DashboardQuery) =>
  useQuery({
    queryKey: ['dashboard', 'weekly-comparison', filters],
    queryFn:  () =>
      api.get<WeeklyComparison[]>('/api/dashboard/weekly-comparison', {
        params: filters,
      }).then(r => r.data),
    staleTime: 0,
  })