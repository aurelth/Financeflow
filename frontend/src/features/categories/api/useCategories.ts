import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import type {
  Category,
  Subcategory,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreateSubcategoryRequest,
  UpdateSubcategoryRequest,
} from '../types/category.types'

// Queries

export const useCategories = () =>
  useQuery({
    queryKey: ['categories'],
    queryFn:  () => api.get<Category[]>('/api/categories').then(r => r.data),
    staleTime: 5 * 60 * 1000,
  })

export const useCategory = (id: string) =>
  useQuery({
    queryKey: ['categories', id],
    queryFn:  () => api.get<Category>(`/api/categories/${id}`).then(r => r.data),
    enabled:  !!id,
    staleTime: 5 * 60 * 1000,
  })

export const useSubcategories = (categoryId: string) =>
  useQuery({
    queryKey: ['categories', categoryId, 'subcategories'],
    queryFn:  () =>
      api.get<Subcategory[]>(`/api/categories/${categoryId}/subcategories`)
        .then(r => r.data),
    enabled:  !!categoryId,
    staleTime: 5 * 60 * 1000,
  })

// Mutations — Categorias

export const useCreateCategory = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateCategoryRequest) =>
      api.post<Category>('/api/categories', data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Categoria criada com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error('Erro ao criar categoria. Tente novamente.')
      }
    },
  })
}

export const useUpdateCategory = (id: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateCategoryRequest) =>
      api.put<Category>(`/api/categories/${id}`, data).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Categoria atualizada com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error('Erro ao atualizar categoria. Tente novamente.')
      }
    },
  })
}

export const useDeleteCategory = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (id: string) =>
      api.delete(`/api/categories/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Categoria removida com sucesso!')
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message
      toast.error(msg ?? 'Erro ao remover categoria. Tente novamente.')
    },
  })
}

// Mutations — Subcategorias

export const useCreateSubcategory = (categoryId: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateSubcategoryRequest) =>
      api.post<Subcategory>(
        `/api/categories/${categoryId}/subcategories`, data
      ).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories', categoryId, 'subcategories'] })
      qc.invalidateQueries({ queryKey: ['categories', categoryId] })
      toast.success('Subcategoria criada com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error('Erro ao criar subcategoria. Tente novamente.')
      }
    },
  })
}

export const useUpdateSubcategory = (categoryId: string, subcategoryId: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateSubcategoryRequest) =>
      api.put<Subcategory>(
        `/api/categories/${categoryId}/subcategories/${subcategoryId}`, data
      ).then(r => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories', categoryId, 'subcategories'] })
      qc.invalidateQueries({ queryKey: ['categories', categoryId] })
      toast.success('Subcategoria atualizada com sucesso!')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error('Erro ao atualizar subcategoria. Tente novamente.')
      }
    },
  })
}

export const useDeleteSubcategory = (categoryId: string) => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (subcategoryId: string) =>
      api.delete(`/api/categories/${categoryId}/subcategories/${subcategoryId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['categories', categoryId, 'subcategories'] })
      qc.invalidateQueries({ queryKey: ['categories', categoryId] })
      toast.success('Subcategoria removida com sucesso!')
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message
      toast.error(msg ?? 'Erro ao remover subcategoria. Tente novamente.')
    },
  })
}