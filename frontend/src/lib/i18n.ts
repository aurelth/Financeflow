import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'

import ptBRCommon       from '@/locales/pt-BR/common.json'
import ptBRAuth         from '@/locales/pt-BR/auth.json'
import ptBRDashboard    from '@/locales/pt-BR/dashboard.json'
import ptBRTransactions from '@/locales/pt-BR/transactions.json'
import ptBRCategories   from '@/locales/pt-BR/categories.json'
import ptBRBudgets      from '@/locales/pt-BR/budgets.json'
import ptBRReports      from '@/locales/pt-BR/reports.json'
import ptBRImports      from '@/locales/pt-BR/imports.json'
import ptBRSettings     from '@/locales/pt-BR/settings.json'
import ptBRAdmin        from '@/locales/pt-BR/admin.json'
import ptBROnboarding   from '@/locales/pt-BR/onboarding.json'

import enUSCommon       from '@/locales/en-US/common.json'
import enUSAuth         from '@/locales/en-US/auth.json'
import enUSDashboard    from '@/locales/en-US/dashboard.json'
import enUSTransactions from '@/locales/en-US/transactions.json'
import enUSCategories   from '@/locales/en-US/categories.json'
import enUSBudgets      from '@/locales/en-US/budgets.json'
import enUSReports      from '@/locales/en-US/reports.json'
import enUSImports      from '@/locales/en-US/imports.json'
import enUSSettings     from '@/locales/en-US/settings.json'
import enUSAdmin        from '@/locales/en-US/admin.json'
import enUSOnboarding   from '@/locales/en-US/onboarding.json'

import esESCommon       from '@/locales/es-ES/common.json'
import esESAuth         from '@/locales/es-ES/auth.json'
import esESDashboard    from '@/locales/es-ES/dashboard.json'
import esESTransactions from '@/locales/es-ES/transactions.json'
import esESCategories   from '@/locales/es-ES/categories.json'
import esESBudgets      from '@/locales/es-ES/budgets.json'
import esESReports      from '@/locales/es-ES/reports.json'
import esESImports      from '@/locales/es-ES/imports.json'
import esESSettings     from '@/locales/es-ES/settings.json'
import esESAdmin        from '@/locales/es-ES/admin.json'
import esESOnboarding   from '@/locales/es-ES/onboarding.json'

import frFRCommon       from '@/locales/fr-FR/common.json'
import frFRAuth         from '@/locales/fr-FR/auth.json'
import frFRDashboard    from '@/locales/fr-FR/dashboard.json'
import frFRTransactions from '@/locales/fr-FR/transactions.json'
import frFRCategories   from '@/locales/fr-FR/categories.json'
import frFRBudgets      from '@/locales/fr-FR/budgets.json'
import frFRReports      from '@/locales/fr-FR/reports.json'
import frFRImports      from '@/locales/fr-FR/imports.json'
import frFRSettings     from '@/locales/fr-FR/settings.json'
import frFRAdmin        from '@/locales/fr-FR/admin.json'
import frFROnboarding   from '@/locales/fr-FR/onboarding.json'

export const SUPPORTED_LANGUAGES = ['pt-BR', 'en-US', 'es-ES', 'fr-FR'] as const
export type SupportedLanguage = typeof SUPPORTED_LANGUAGES[number]

export const LANGUAGE_LABELS: Record<SupportedLanguage, string> = {
  'pt-BR': 'Português (Brasil)',
  'en-US': 'English (US)',
  'es-ES': 'Español',
  'fr-FR': 'Français',
}

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      'pt-BR': {
        common:       ptBRCommon,
        auth:         ptBRAuth,
        dashboard:    ptBRDashboard,
        transactions: ptBRTransactions,
        categories:   ptBRCategories,
        budgets:      ptBRBudgets,
        reports:      ptBRReports,
        imports:      ptBRImports,
        settings:     ptBRSettings,
        admin:        ptBRAdmin,
        onboarding:   ptBROnboarding,
      },
      'en-US': {
        common:       enUSCommon,
        auth:         enUSAuth,
        dashboard:    enUSDashboard,
        transactions: enUSTransactions,
        categories:   enUSCategories,
        budgets:      enUSBudgets,
        reports:      enUSReports,
        imports:      enUSImports,
        settings:     enUSSettings,
        admin:        enUSAdmin,
        onboarding:   enUSOnboarding,
      },
      'es-ES': {
        common:       esESCommon,
        auth:         esESAuth,
        dashboard:    esESDashboard,
        transactions: esESTransactions,
        categories:   esESCategories,
        budgets:      esESBudgets,
        reports:      esESReports,
        imports:      esESImports,
        settings:     esESSettings,
        admin:        esESAdmin,
        onboarding:   esESOnboarding,
      },
      'fr-FR': {
        common:       frFRCommon,
        auth:         frFRAuth,
        dashboard:    frFRDashboard,
        transactions: frFRTransactions,
        categories:   frFRCategories,
        budgets:      frFRBudgets,
        reports:      frFRReports,
        imports:      frFRImports,
        settings:     frFRSettings,
        admin:        frFRAdmin,
        onboarding:   frFROnboarding,
      },
    },
    detection: {
      order: ['localStorage', 'navigator'],
      lookupLocalStorage: 'ff_language',
      caches: ['localStorage'],
    },
    fallbackLng: 'en-US',
    supportedLngs: SUPPORTED_LANGUAGES,
    defaultNS: 'common',
    ns: [
      'common', 'auth', 'dashboard', 'transactions', 'categories',
      'budgets', 'reports', 'imports', 'settings', 'admin', 'onboarding',
    ],
    interpolation: {
      escapeValue: false,
    },
  })

export default i18n