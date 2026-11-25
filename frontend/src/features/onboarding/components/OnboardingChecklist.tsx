import { Link } from 'react-router-dom'
import { Check, ChevronRight, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useOnboardingChecklist, useSetChecklistDismissed } from '../api/useOnboardingChecklist'
import { CHECKLIST_STEP_META } from './checklistSteps'

// Cartão "Primeiros passos" do Dashboard. Some sozinho enquanto carrega, em caso de erro
// (é um complemento, não deve atrapalhar a tela) e depois de dispensado.
export default function OnboardingChecklist() {
  const { t } = useTranslation('onboarding')
  const { data } = useOnboardingChecklist()
  const { mutate: setDismissed, isPending } = useSetChecklistDismissed()

  if (!data || data.dismissed) return null

  const allDone = data.completed >= data.total
  const percent = data.total === 0 ? 0 : Math.round((data.completed / data.total) * 100)

  return (
    <section
      aria-labelledby="onboarding-checklist-title"
      className="rounded-2xl p-5 space-y-4"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h2 id="onboarding-checklist-title" className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
            {allDone ? t('checklist.doneTitle') : t('checklist.title')}
          </h2>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            {allDone
              ? t('checklist.doneDescription')
              : t('checklist.progress', { done: data.completed, total: data.total })}
          </p>
        </div>
        <button
          type="button"
          onClick={() => setDismissed(true)}
          disabled={isPending}
          aria-label={t('checklist.dismiss')}
          title={t('checklist.dismiss')}
          className="p-1.5 -m-1.5 rounded-lg transition-colors disabled:opacity-50"
          style={{ color: 'var(--ff-text-muted)' }}
        >
          <X size={16} />
        </button>
      </div>

      <div
        role="progressbar"
        aria-label={t('checklist.title')}
        aria-valuemin={0}
        aria-valuemax={data.total}
        aria-valuenow={data.completed}
        className="h-1.5 rounded-full overflow-hidden"
        style={{ background: 'var(--ff-bg-elevated)' }}
      >
        <div
          className="h-full rounded-full transition-[width] duration-500"
          style={{ width: `${percent}%`, background: 'var(--ff-emerald)' }}
        />
      </div>

      {!allDone && (
        <ul className="grid gap-2 sm:grid-cols-2">
          {data.steps.map(step => {
            const meta = CHECKLIST_STEP_META[step.key]
            if (!meta) return null
            const Icon = meta.icon

            return (
              <li key={step.key}>
                <Link
                  to={meta.to}
                  className="flex items-center gap-3 rounded-xl px-3 py-2.5 transition-colors hover:brightness-125 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--ff-emerald)]"
                  style={{ background: 'var(--ff-bg-elevated)' }}
                >
                  <span
                    className="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
                    style={{
                      background: step.done ? 'var(--ff-emerald)' : 'var(--ff-bg-card)',
                      color:      step.done ? 'var(--ff-emerald-subtle)' : 'var(--ff-text-muted)',
                    }}
                  >
                    {step.done ? <Check size={16} /> : <Icon size={16} />}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span
                      className={`block text-sm font-medium ${step.done ? 'line-through' : ''}`}
                      style={{ color: step.done ? 'var(--ff-text-muted)' : 'var(--ff-text-primary)' }}
                    >
                      {t(`checklist.steps.${step.key}.title`)}
                      {step.done && <span className="sr-only"> — {t('checklist.completed')}</span>}
                    </span>
                    {!step.done && (
                      <span className="block text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
                        {t(`checklist.steps.${step.key}.description`)}
                      </span>
                    )}
                  </span>
                  {!step.done && <ChevronRight size={16} style={{ color: 'var(--ff-text-muted)' }} />}
                </Link>
              </li>
            )
          })}
        </ul>
      )}

      {allDone && (
        <button
          type="button"
          onClick={() => setDismissed(true)}
          disabled={isPending}
          className="h-9 px-4 rounded-xl text-sm font-medium disabled:opacity-50"
          style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
        >
          {t('checklist.close')}
        </button>
      )}
    </section>
  )
}
