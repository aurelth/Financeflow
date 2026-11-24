import type { TransactionType } from '../../categories/types/category.types'
import type { TransactionStatus } from './transaction.types'

// Espelha InstallmentDtos.cs. Enums chegam como string ou número conforme o endpoint: use resolveType/resolveStatus.

export interface Installment {
  id:     string
  number: number
  date:   string
  amount: number
  status: TransactionStatus | string
}

export interface InstallmentPlan {
  groupId:         string
  description:     string
  type:            TransactionType | string
  count:           number
  totalAmount:     number
  paidAmount:      number
  remainingAmount: number
  installments:    Installment[]
}

export interface CancelInstallmentsResult {
  cancelledCount: number
  keptPaidCount:  number
}
