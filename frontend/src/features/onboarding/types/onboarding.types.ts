// Chaves estáveis enviadas pelo servidor; cada uma tem texto e destino em components/checklistSteps.ts
export type ChecklistStepKey = 'transaction' | 'import' | 'budget' | 'goal'

export interface ChecklistStep {
  key:  ChecklistStepKey
  done: boolean
}

export interface OnboardingChecklist {
  steps:     ChecklistStep[]
  completed: number
  total:     number
  dismissed: boolean
}
