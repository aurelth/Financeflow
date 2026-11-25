import { ArrowLeftRight, FileUp, PiggyBank, Target, type LucideIcon } from 'lucide-react'
import type { ChecklistStepKey } from '../types/onboarding.types'

// Destino e ícone de cada passo. O texto vem das traduções (onboarding:checklist.steps.<key>).
// Um passo novo no servidor sem entrada aqui é ignorado, em vez de quebrar a tela.
export const CHECKLIST_STEP_META: Record<ChecklistStepKey, { to: string; icon: LucideIcon }> = {
  transaction: { to: '/transactions', icon: ArrowLeftRight },
  import:      { to: '/imports',      icon: FileUp },
  budget:      { to: '/budgets',      icon: PiggyBank },
  goal:        { to: '/goals',        icon: Target },
}
