import { ListChecks } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useOnboardingChecklist, useSetChecklistDismissed } from '../api/useOnboardingChecklist'

// Aparece (em Configurações) só depois de o checklist ser dispensado
export default function ChecklistRestoreButton() {
  const { t } = useTranslation('onboarding')
  const { data } = useOnboardingChecklist()
  const { mutate: setDismissed, isPending } = useSetChecklistDismissed()

  if (!data?.dismissed) return null

  return (
    <button
      type="button"
      onClick={() => setDismissed(false)}
      disabled={isPending}
      className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
      style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
    >
      <ListChecks size={15} />
      {t('checklist.restore')}
    </button>
  )
}
