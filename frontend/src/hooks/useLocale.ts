import { useTranslation } from 'react-i18next'
import { useAuthStore }   from '@/store/authStore'
import { formatCurrency, formatDate, formatDateTime, formatMonthYear, formatNumber } from '@/lib/format'
import type { SupportedLanguage } from '@/lib/i18n'

export function useLocale() {
  const { i18n }   = useTranslation()
  const user        = useAuthStore(s => s.user)

  const language = (i18n.language ?? 'en-US') as SupportedLanguage
  const currency = user?.currency ?? 'BRL'

  return {
    language,
    currency,
    formatCurrency: (value: number) =>
      formatCurrency(value, language, currency),
    formatDate: (date: string | Date) =>
      formatDate(date, language),
    formatDateTime: (date: string | Date) =>
      formatDateTime(date, language),
    formatMonthYear: (month: number, year: number) =>
      formatMonthYear(month, year, language),
    formatNumber: (value: number, decimals?: number) =>
      formatNumber(value, language, decimals),
  }
}