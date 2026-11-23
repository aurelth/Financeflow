import React from 'react'
import { vi } from 'vitest'
import ptBR from '@/locales/pt-BR/accounts.json'

// Resolve chaves do namespace "accounts" com os textos reais em pt-BR (incluindo interpolação
// e plural), para os testes poderem afirmar sobre o texto que o utilizador vê.
function resolve(key: string, options?: Record<string, unknown>): string {
  const path = (key.includes(':') ? key.split(':')[1] : key).split('.')
  const lookup = (k: string[]) =>
    k.reduce<unknown>((node, part) => (node as Record<string, unknown> | undefined)?.[part], ptBR)

  let value = lookup(path)
  if (value === undefined && typeof options?.count === 'number') {
    const plural = options.count === 1 ? '_one' : '_other'
    value = lookup([...path.slice(0, -1), `${path[path.length - 1]}${plural}`])
  }
  if (typeof value !== 'string') return key

  return value.replace(/\{\{(\w+)\}\}/g, (_, name) => String(options?.[name] ?? ''))
}

export const accountsI18nMock = {
  useTranslation: () => ({
    t: resolve,
    i18n: { language: 'pt-BR', changeLanguage: vi.fn() },
  }),
  Trans: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  initReactI18next: { type: '3rdParty', init: vi.fn() },
}
