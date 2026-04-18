// Palavras-chave para detecção de transferências
const TRANSFER_KEYWORDS = [
  'TRANSFERENCIA PIX',
  'TRANSFERENCIA',
  'TED-TRANSF',
  'TED',
  'PIX',
]

// Verifica se uma descrição sugere uma transferência entre contas
export function isLikelyTransfer(description: string): boolean {
  const upper = description.toUpperCase().trim()
  return TRANSFER_KEYWORDS.some(keyword => upper.includes(keyword))
}