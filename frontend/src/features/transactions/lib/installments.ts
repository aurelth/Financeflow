// Parcelamento no cliente. A divisão espelha o back-end (InstallmentPlanner): em centavos, com a sobra
// distribuída uma a uma nas primeiras parcelas, para a prévia mostrar exatamente o que será gravado.

export const MIN_INSTALLMENTS = 2
export const MAX_INSTALLMENTS = 60

export function isValidInstallmentCount(count: number): boolean {
  return Number.isInteger(count) && count >= MIN_INSTALLMENTS && count <= MAX_INSTALLMENTS
}

export function splitInstallments(total: number, count: number): number[] {
  if (!Number.isInteger(count) || count < 1) return []

  const totalCents = Math.round(total * 100)
  const base  = Math.floor(totalCents / count)
  const extra = totalCents - base * count

  return Array.from({ length: count }, (_, i) => (base + (i < extra ? 1 : 0)) / 100)
}

// Cada parcela precisa valer ao menos um centavo
export function isTotalEnoughFor(total: number, count: number): boolean {
  return Math.round(total * 100) >= count
}

export function formatBRL(value: number): string {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}
