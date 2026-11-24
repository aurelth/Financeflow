import { describe, it, expect } from 'vitest'
import {
  MAX_INSTALLMENTS, MIN_INSTALLMENTS, formatBRL, isTotalEnoughFor, isValidInstallmentCount, splitInstallments,
} from '@/features/transactions/lib/installments'

describe('installments (helper)', () => {
  it('divide igualmente quando o total é múltiplo do número de parcelas', () => {
    expect(splitInstallments(300, 3)).toEqual([100, 100, 100])
  })

  it('distribui a sobra de centavos nas primeiras parcelas (100/7)', () => {
    const parts = splitInstallments(100, 7)

    expect(parts).toEqual([14.29, 14.29, 14.29, 14.29, 14.28, 14.28, 14.28])
    expect(Math.round(parts.reduce((s, p) => s + p, 0) * 100)).toBe(10000)
  })

  it('a soma das parcelas sempre fecha com o total, mesmo com ponto flutuante chato', () => {
    for (const [total, count] of [[0.1 + 0.2, 3], [1234.56, 12], [99.99, 60], [0.6, 60]] as const) {
      const cents = splitInstallments(total, count).reduce((s, p) => s + Math.round(p * 100), 0)
      expect(cents).toBe(Math.round(total * 100))
    }
  })

  it('devolve lista vazia para quantidade inválida', () => {
    expect(splitInstallments(100, 0)).toEqual([])
    expect(splitInstallments(100, 2.5)).toEqual([])
  })

  it('aceita de 2 a 60 parcelas, inteiras', () => {
    expect(isValidInstallmentCount(MIN_INSTALLMENTS)).toBe(true)
    expect(isValidInstallmentCount(MAX_INSTALLMENTS)).toBe(true)
    expect(isValidInstallmentCount(1)).toBe(false)
    expect(isValidInstallmentCount(61)).toBe(false)
    expect(isValidInstallmentCount(3.5)).toBe(false)
    expect(isValidInstallmentCount(Number.NaN)).toBe(false)
  })

  it('exige ao menos um centavo por parcela', () => {
    expect(isTotalEnoughFor(0.1, 10)).toBe(true)
    expect(isTotalEnoughFor(0.09, 10)).toBe(false)
  })

  it('formata em reais', () => {
    expect(formatBRL(1234.5).replace(/\s/g, ' ')).toBe('R$ 1.234,50')
  })
})
