import { useState } from 'react'
import { Loader2, Tags, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useCategoryRules, useDeleteCategoryRule } from '../api/useCategoryRules'

// Acima disso a lista vira rolagem infinita de configurações: o resto fica atrás de "ver todas"
const COLLAPSED_LIMIT = 8

export default function CategoryRulesCard() {
  const { t } = useTranslation('settings')
  const { data: rules, isLoading, isError } = useCategoryRules()
  const deleteRule = useDeleteCategoryRule()
  const [showAll, setShowAll] = useState(false)

  const total    = rules?.length ?? 0
  const visible  = showAll ? rules ?? [] : (rules ?? []).slice(0, COLLAPSED_LIMIT)
  const hasMore  = total > COLLAPSED_LIMIT

  return (
    <div
      className="rounded-2xl p-6"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex items-center gap-2 mb-2">
        <Tags size={16} style={{ color: 'var(--ff-emerald)' }} />
        <h2 className="font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
          {t('categoryRules.title')}
        </h2>
      </div>
      <p className="text-xs mb-5" style={{ color: 'var(--ff-text-muted)' }}>
        {t('categoryRules.subtitle')}
      </p>

      {isLoading && (
        <div className="flex justify-center py-4">
          <Loader2 size={18} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
        </div>
      )}

      {isError && (
        <p role="alert" className="text-sm" style={{ color: 'var(--ff-expense)' }}>
          {t('categoryRules.error')}
        </p>
      )}

      {!isLoading && !isError && total === 0 && (
        <p className="text-sm" style={{ color: 'var(--ff-text-muted)' }}>
          {t('categoryRules.empty')}
        </p>
      )}

      {total > 0 && (
        <ul className="divide-y" style={{ borderColor: 'var(--ff-border)' }}>
          {visible.map(rule => (
            <li
              key={rule.id}
              className="flex items-center gap-3 py-2.5"
              style={{ borderColor: 'var(--ff-border)' }}
            >
              <div className="flex-1 min-w-0">
                <p
                  className="text-sm font-medium truncate capitalize"
                  style={{ color: 'var(--ff-text-primary)' }}
                >
                  {rule.key}
                </p>
                <p className="text-xs truncate" style={{ color: 'var(--ff-text-muted)' }}>
                  <span aria-hidden="true">→ </span>
                  <span style={{ color: rule.categoryColor }}>
                    {rule.categoryIcon} {rule.categoryName}
                  </span>
                  {' · '}
                  {t(`categoryRules.type.${rule.type}`)}
                  {' · '}
                  {t('categoryRules.hits', { count: rule.hitCount })}
                </p>
              </div>

              <button
                type="button"
                onClick={() => deleteRule.mutate(rule.id)}
                disabled={deleteRule.isPending && deleteRule.variables === rule.id}
                aria-label={t('categoryRules.forget', { key: rule.key })}
                title={t('categoryRules.forgetTitle')}
                className="p-1.5 rounded-lg transition-colors disabled:opacity-50"
                style={{ color: 'var(--ff-text-muted)' }}
              >
                <X size={15} />
              </button>
            </li>
          ))}
        </ul>
      )}

      {hasMore && (
        <button
          type="button"
          onClick={() => setShowAll(v => !v)}
          className="mt-3 text-xs font-medium"
          style={{ color: 'var(--ff-emerald)' }}
        >
          {showAll
            ? t('categoryRules.showLess')
            : t('categoryRules.showAll', { count: total })}
        </button>
      )}
    </div>
  )
}
