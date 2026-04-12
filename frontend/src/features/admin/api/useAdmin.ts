import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import type {
  AdminUserList,
  AdminCategory,
  AdminMetrics,
  CreateDefaultCategoryRequest,
  UpdateDefaultCategoryRequest,
} from '../types/admin.types'

const getApiError = (err: any, fallback: string): string => {
  const errors = err?.response?.data?.errors
  if (errors) return Object.values(errors).flat().join(' ')
  return err?.response?.data?.message ?? err?.response?.data?.title ?? fallback
}

// GET /api/admin/users
export function useAdminUsers(
  page     = 1,
  pageSize = 20,
  search?: string,
  isActive?: boolean
) {
  return useQuery({
    queryKey: ['admin', 'users', page, pageSize, search, isActive],
    queryFn:  async () => {
      const params = new URLSearchParams()
      params.set('page',     String(page))
      params.set('pageSize', String(pageSize))
      if (search   !== undefined) params.set('search',   search)
      if (isActive !== undefined) params.set('isActive', String(isActive))

      const { data } = await api.get<AdminUserList>(`/api/admin/users?${params}`)
      return data
    },
  })
}

// PATCH /api/admin/users/{id}/deactivate
export function useDeactivateUser() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.patch(`/api/admin/users/${id}/deactivate`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast.success('Usuário desativado com sucesso.')
    },
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao desativar usuário.'))
    },
  })
}

// PATCH /api/admin/users/{id}/reactivate
export function useReactivateUser() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.patch(`/api/admin/users/${id}/reactivate`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast.success('Usuário reativado com sucesso.')
    },
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao reativar usuário.'))
    },
  })
}

// PATCH /api/admin/users/{id}/promote
export function usePromoteUser() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.patch(`/api/admin/users/${id}/promote`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast.success('Usuário promovido a Admin.')
    },
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao promover usuário.'))
    },
  })
}

// PATCH /api/admin/users/{id}/demote
export function useDemoteUser() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.patch(`/api/admin/users/${id}/demote`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast.success('Admin rebaixado a usuário comum.')
    },
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao rebaixar Admin.'))
    },
  })
}

// GET /api/admin/categories
export function useAdminCategories() {
  return useQuery({
    queryKey: ['admin', 'categories'],
    queryFn:  async () => {
      const { data } = await api.get<AdminCategory[]>('/api/admin/categories')
      return data
    },
  })
}

// POST /api/admin/categories
export function useCreateDefaultCategory() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (body: CreateDefaultCategoryRequest) =>
      api.post<AdminCategory>('/api/admin/categories', body).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'categories'] })
      qc.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Categoria padrão criada com sucesso.')
    },
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao criar categoria padrão.'))
    },
  })
}

// PUT /api/admin/categories/{id}
export function useUpdateDefaultCategory() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: ({ id, ...body }: UpdateDefaultCategoryRequest & { id: string }) =>
      api.put<AdminCategory>(`/api/admin/categories/${id}`, body).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'categories'] })
      qc.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Categoria padrão atualizada com sucesso.')
    },
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao atualizar categoria padrão.'))
    },
  })
}

// DELETE /api/admin/categories/{id}
export function useDeleteDefaultCategory() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => api.delete(`/api/admin/categories/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'categories'] })
      qc.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Categoria padrão excluída com sucesso.')
    },
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao excluir categoria padrão.'))
    },
  })
}

// GET /api/admin/metrics
export function useAdminMetrics() {
  return useQuery({
    queryKey: ['admin', 'metrics'],
    queryFn:  async () => {
      const { data } = await api.get<AdminMetrics>('/api/admin/metrics')
      return data
    },
  })
}