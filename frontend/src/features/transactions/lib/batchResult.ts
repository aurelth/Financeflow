import { toast } from 'sonner'
import type { BatchTransactionResult } from '../types/transaction.types'

export type BatchAction = 'update' | 'delete'

function plural(count: number, singular: string, pluralForm: string) {
  return count === 1 ? singular : pluralForm
}

// "3 transações atualizadas." / "1 transação removida."
export function batchSuccessMessage(count: number, action: BatchAction): string {
  const noun = plural(count, 'transação', 'transações')
  const verb = action === 'delete'
    ? plural(count, 'removida', 'removidas')
    : plural(count, 'atualizada', 'atualizadas')
  return `${count} ${noun} ${verb}.`
}

// Agrupa os ignorados por motivo: "2× A categoria escolhida é de outro tipo (receita/despesa)."
export function batchSkippedMessage(result: BatchTransactionResult): string {
  const byReason = new Map<string, number>()
  for (const item of result.skipped)
    byReason.set(item.reason, (byReason.get(item.reason) ?? 0) + 1)

  const count  = result.skipped.length
  const header = `${count} ${plural(count, 'transação ficou', 'transações ficaram')} de fora:`
  const reasons = [...byReason.entries()].map(([reason, n]) => `${n}× ${reason}`)

  return `${header} ${reasons.join(' ')}`
}

// Avisa o que deu certo e, à parte, o que ficou de fora e por quê
export function reportBatchResult(result: BatchTransactionResult, action: BatchAction) {
  if (result.succeeded > 0)
    toast.success(batchSuccessMessage(result.succeeded, action))

  if (result.skipped.length > 0)
    toast.warning(batchSkippedMessage(result), { duration: 9000 })
}
