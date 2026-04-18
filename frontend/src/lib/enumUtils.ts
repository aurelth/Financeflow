import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType } from '@/features/transactions/types/transaction.types'

// Normaliza TransactionType que pode chegar como string ou número
export function resolveType(type: TransactionType | string): TransactionType {
  if (typeof type === 'number') return type as TransactionType
  const map: Record<string, TransactionType> = {
    'Income':   TransactionType.Income,
    'Expense':  TransactionType.Expense,
    'Transfer': TransactionType.Transfer,
  }
  return map[type] ?? TransactionType.Expense
}

// Adicionado: normaliza TransactionStatus que pode chegar como string ou número
export function resolveStatus(status: TransactionStatus | string): TransactionStatus {
  if (typeof status === 'number') return status as TransactionStatus
  const map: Record<string, TransactionStatus> = {
    'Paid':      TransactionStatus.Paid,
    'Pending':   TransactionStatus.Pending,
    'Scheduled': TransactionStatus.Scheduled,
  }
  return map[status] ?? TransactionStatus.Paid
}

// Normaliza RecurrenceType que pode chegar como string ou número
export function resolveRecurrence(type: RecurrenceType | string): RecurrenceType {
  if (typeof type === 'number') return type as RecurrenceType
  const map: Record<string, RecurrenceType> = {
    'None':    RecurrenceType.None,
    'Daily':   RecurrenceType.Daily,
    'Weekly':  RecurrenceType.Weekly,
    'Monthly': RecurrenceType.Monthly,
    'Yearly':  RecurrenceType.Yearly,
  }
  return map[type] ?? RecurrenceType.None
}