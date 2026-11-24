import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/axios'
import type { AiQuota, QuickEntryResult } from '../types/quickEntry.types'

export const AI_QUOTA_KEY = ['ai-quota'] as const

export const useAiQuota = () =>
  useQuery({
    queryKey:  AI_QUOTA_KEY,
    queryFn:   () => api.get<AiQuota>('/api/quickentry/quota').then(r => r.data),
    // A cota muda a cada chamada e vira o dia sozinha: sempre confere ao abrir o diálogo
    staleTime: 0,
  })

interface ParseInput {
  text?:  string
  image?: File | null
}

// Multipart: o texto vai como campo e a foto como arquivo. Sem toast aqui: o diálogo mostra o erro
// no próprio lugar (429/422 têm mensagem específica do servidor).
export const useParseQuickEntry = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: ({ text, image }: ParseInput) => {
      const formData = new FormData()
      if (text?.trim()) formData.append('text', text.trim())
      if (image)        formData.append('image', image)

      return api.post<QuickEntryResult>('/api/quickentry/parse', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      }).then(r => r.data)
    },
    onSuccess: result => {
      // A resposta já traz a cota atualizada: evita uma ida extra ao servidor
      qc.setQueryData(AI_QUOTA_KEY, result.quota)
    },
    // Sucesso ou erro (429, falha da IA com a cota devolvida): o servidor é a fonte da verdade
    onSettled: () => { qc.invalidateQueries({ queryKey: AI_QUOTA_KEY }) },
  })
}
