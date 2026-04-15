import { useQuery } from '@tanstack/react-query'
import api from '@/lib/axios'
import type {
  CashFlowDto,
  AnnualSummaryDto,
  ReportByCategoryDto,
  ReportByTagDto,
  ProjectionsDto,
  CashFlowParams,
  ByCategoryParams,
  ByTagParams,
  ProjectionsParams,
} from '../types/analytics.types'

// GET /api/analytics/cash-flow
export const useCashFlow = (params: CashFlowParams, enabled = true) =>
  useQuery<CashFlowDto>({
    queryKey: ['analytics', 'cash-flow', params],
    queryFn: () =>
      api.get<CashFlowDto>('/api/analytics/cash-flow', { params }).then(r => r.data),
    enabled,
    staleTime: 5 * 60 * 1000,
  })

// GET /api/analytics/annual-summary
export const useAnnualSummary = (year: number, enabled = true) =>
  useQuery<AnnualSummaryDto>({
    queryKey: ['analytics', 'annual-summary', year],
    queryFn: () =>
      api.get<AnnualSummaryDto>('/api/analytics/annual-summary', { params: { year } }).then(r => r.data),
    enabled,
    staleTime: 5 * 60 * 1000,
  })

// GET /api/analytics/by-category
export const useReportByCategory = (params: ByCategoryParams, enabled = true) =>
  useQuery<ReportByCategoryDto>({
    queryKey: ['analytics', 'by-category', params],
    queryFn: () =>
      api.get<ReportByCategoryDto>('/api/analytics/by-category', { params }).then(r => r.data),
    enabled,
    staleTime: 5 * 60 * 1000,
  })

// GET /api/analytics/by-tag
export const useReportByTag = (params: ByTagParams, enabled = true) =>
  useQuery<ReportByTagDto>({
    queryKey: ['analytics', 'by-tag', params],
    queryFn: () =>
      api.get<ReportByTagDto>('/api/analytics/by-tag', { params }).then(r => r.data),
    enabled,
    staleTime: 5 * 60 * 1000,
  })

// GET /api/analytics/projections
export const useProjections = (params: ProjectionsParams, enabled = true) =>
  useQuery<ProjectionsDto>({
    queryKey: ['analytics', 'projections', params],
    queryFn: () =>
      api.get<ProjectionsDto>('/api/analytics/projections', { params }).then(r => r.data),
    enabled,
    staleTime: 5 * 60 * 1000,
  })