import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/apiError'
import type { CategoryRule } from '../types/settings.types'

export const CATEGORY_RULES_KEY = ['category-rules'] as const

// GET /api/categoryrules
export function useCategoryRules() {
  return useQuery({
    queryKey: CATEGORY_RULES_KEY,
    queryFn: async () => {
      const { data } = await api.get<CategoryRule[]>('/api/categoryrules')
      return data
    },
  })
}

// DELETE /api/categoryrules/{id}
export function useDeleteCategoryRule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/categoryrules/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CATEGORY_RULES_KEY })
    },
    onError: (err: unknown) => {
      toast.error(getApiErrorMessage(err, 'Erro ao esquecer a regra.'))
    },
  })
}
