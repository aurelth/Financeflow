import type { SupportedLanguage } from './i18n'

// ── Mapeamento de moeda para locale de formatação ─────────────────────────────
// O idioma controla os separadores, a moeda controla o símbolo
const CURRENCY_LOCALES: Record<string, string> = {
  BRL: 'pt-BR',
  USD: 'en-US',
  EUR: 'fr-FR',
  GBP: 'en-GB',
  ARS: 'es-AR',
  MXN: 'es-MX',
  CLP: 'es-CL',
  COP: 'es-CO',
}

// ── Separadores decimais por idioma ───────────────────────────────────────────
const DECIMAL_SEPARATORS: Record<SupportedLanguage, string> = {
  'pt-BR': ',',
  'en-US': '.',
  'es-ES': ',',
  'fr-FR': ',',
}

const THOUSAND_SEPARATORS: Record<SupportedLanguage, string> = {
  'pt-BR': '.',
  'en-US': ',',
  'es-ES': '.',
  'fr-FR': ' ',
}

/**
 * Formata um valor monetário combinando:
 * - Símbolo da moeda (definido pela currency do perfil)
 * - Separadores (definidos pelo idioma escolhido)
 *
 * Exemplos:
 * formatCurrency(1500, 'en-US', 'BRL') → 'R$ 1,500.00'
 * formatCurrency(1500, 'pt-BR', 'USD') → '$ 1.500,00'
 * formatCurrency(1500, 'fr-FR', 'EUR') → '1 500,00 €'
 */
export function formatCurrency(
  value: number,
  language: SupportedLanguage | string,
  currency: string = 'BRL'
): string {
  const lang = (language as SupportedLanguage) in DECIMAL_SEPARATORS
    ? (language as SupportedLanguage)
    : 'en-US'

  const decimalSep  = DECIMAL_SEPARATORS[lang]
  const thousandSep = THOUSAND_SEPARATORS[lang]

  // Formata o número absoluto com 2 casas decimais
  const absValue   = Math.abs(value)
  const [intPart, decPart] = absValue.toFixed(2).split('.')

  // Aplica separador de milhar
  const formattedInt = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, thousandSep)
  const formattedNum = `${formattedInt}${decimalSep}${decPart}`

  // Obtém o símbolo da moeda usando o locale nativo da moeda
  const currencyLocale = CURRENCY_LOCALES[currency.toUpperCase()] ?? 'en-US'
  const symbol = getCurrencySymbol(currency, currencyLocale)

  // Posição do símbolo — EUR e algumas moedas vão após o valor
  const symbolAfter = ['EUR', 'CHF'].includes(currency.toUpperCase())
  const signed      = value < 0 ? '-' : ''

  if (symbolAfter) {
    return `${signed}${formattedNum} ${symbol}`
  }

  return `${signed}${symbol} ${formattedNum}`
}

/**
 * Extrai apenas o símbolo da moeda sem formatação numérica
 */
function getCurrencySymbol(currency: string, locale: string): string {
  try {
    const formatted = new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: currency.toUpperCase(),
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(0)

    // Remove o zero e espaços, mantém apenas o símbolo
    return formatted.replace(/[\d\s,.']/g, '').trim()
  } catch {
    return currency
  }
}

/**
 * Formata uma data de acordo com o idioma
 *
 * Exemplos:
 * formatDate('2026-03-15', 'en-US') → '03/15/2026'
 * formatDate('2026-03-15', 'pt-BR') → '15/03/2026'
 * formatDate('2026-03-15', 'fr-FR') → '15/03/2026'
 */
export function formatDate(
  date: string | Date,
  language: SupportedLanguage | string
): string {
  const lang = (language as SupportedLanguage) in DECIMAL_SEPARATORS
    ? (language as SupportedLanguage)
    : 'en-US'

  const d = typeof date === 'string'
    ? new Date(`${date.includes('T') ? date : `${date}T00:00:00`}`)
    : date

  if (isNaN(d.getTime())) return '—'

  return d.toLocaleDateString(lang, {
    day:   '2-digit',
    month: '2-digit',
    year:  'numeric',
  })
}

/**
 * Formata uma data com hora
 */
export function formatDateTime(
  date: string | Date,
  language: SupportedLanguage | string
): string {
  const lang = (language as SupportedLanguage) in DECIMAL_SEPARATORS
    ? (language as SupportedLanguage)
    : 'en-US'

  const d = typeof date === 'string'
    ? new Date(`${date.includes('T') ? date : `${date}T00:00:00`}`)
    : date

  if (isNaN(d.getTime())) return '—'

  return d.toLocaleString(lang, {
    day:    '2-digit',
    month:  '2-digit',
    year:   'numeric',
    hour:   '2-digit',
    minute: '2-digit',
  })
}

/**
 * Formata um mês/ano por extenso de acordo com o idioma
 *
 * Exemplos:
 * formatMonthYear(3, 2026, 'pt-BR') → 'março de 2026'
 * formatMonthYear(3, 2026, 'en-US') → 'March 2026'
 * formatMonthYear(3, 2026, 'fr-FR') → 'mars 2026'
 */
export function formatMonthYear(
  month: number,
  year: number,
  language: SupportedLanguage | string
): string {
  const lang = (language as SupportedLanguage) in DECIMAL_SEPARATORS
    ? (language as SupportedLanguage)
    : 'en-US'

  return new Date(year, month - 1).toLocaleString(lang, {
    month: 'long',
    year:  'numeric',
  })
}

/**
 * Formata um número simples com separadores do idioma
 */
export function formatNumber(
  value: number,
  language: SupportedLanguage | string,
  decimals: number = 2
): string {
  const lang = (language as SupportedLanguage) in DECIMAL_SEPARATORS
    ? (language as SupportedLanguage)
    : 'en-US'

  return value.toLocaleString(lang, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  })
}