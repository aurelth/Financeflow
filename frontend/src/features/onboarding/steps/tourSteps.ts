import type { DriveStep } from 'driver.js'

// Adicionado: passos do tour com IDs dos elementos alvo na sidebar e dashboard
export function getTourSteps(t: (key: string) => string): DriveStep[] {
  return [
    {
      // Passo 1 — Bem-vindo (sem elemento alvo — popover centrado)
      popover: {
        title:       t('onboarding:welcome.title'),
        description: t('onboarding:welcome.description'),
        side:        'over',
        align:       'center',
      },
    },
    {
      // Passo 2 — Dashboard
      element: 'a[href="/dashboard"]',
      popover: {
        title:       t('onboarding:dashboard.title'),
        description: t('onboarding:dashboard.description'),
        side:        'right',
        align:       'start',
      },
    },
    {
      // Passo 3 — Transações
      element: 'a[href="/transactions"]',
      popover: {
        title:       t('onboarding:transactions.title'),
        description: t('onboarding:transactions.description'),
        side:        'right',
        align:       'start',
      },
    },
    {
      // Passo 4 — Categorias
      element: 'a[href="/categories"]',
      popover: {
        title:       t('onboarding:categories.title'),
        description: t('onboarding:categories.description'),
        side:        'right',
        align:       'start',
      },
    },
    {
      // Passo 5 — Orçamentos
      element: 'a[href="/budgets"]',
      popover: {
        title:       t('onboarding:budgets.title'),
        description: t('onboarding:budgets.description'),
        side:        'right',
        align:       'start',
      },
    },
  ]
}