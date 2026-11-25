import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/axios'
import type { OnboardingChecklist } from '../types/onboarding.types'

const CHECKLIST_KEY = ['onboarding-checklist'] as const

export const useOnboardingChecklist = () =>
  useQuery({
    queryKey: CHECKLIST_KEY,
    queryFn:  () => api.get<OnboardingChecklist>('/api/onboarding/checklist').then(r => r.data),
    // O progresso muda em outras telas (criar orçamento, meta...): refaz ao voltar ao Dashboard
    staleTime: 0,
  })

export const useSetChecklistDismissed = () => {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (dismissed: boolean) =>
      api.put('/api/onboarding/checklist/dismissed', { dismissed }),

    // Otimista: o cartão some/volta na hora; se falhar, volta ao que o servidor tinha
    onMutate: async (dismissed) => {
      await qc.cancelQueries({ queryKey: CHECKLIST_KEY })
      const previous = qc.getQueryData<OnboardingChecklist>(CHECKLIST_KEY)
      if (previous) qc.setQueryData<OnboardingChecklist>(CHECKLIST_KEY, { ...previous, dismissed })
      return { previous }
    },
    onError: (_err, _dismissed, context) => {
      if (context?.previous) qc.setQueryData(CHECKLIST_KEY, context.previous)
    },
    onSettled: () => qc.invalidateQueries({ queryKey: CHECKLIST_KEY }),
  })
}
