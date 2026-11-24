import type { TransactionType } from '@/features/categories/types/category.types'
import type { TransferDirection } from '@/features/transactions/types/transaction.types'

// Uma ponta de transferência (um lançamento numa das contas)
export interface TransferSide {
  transactionId: string
  accountId:     string
  accountName:   string | null
  date:          string
  amount:        number
  description:   string
  // A API serializa enums como string
  type:          TransactionType | string
  direction:     TransferDirection | null
}

export type TransferLinkOrigin = 'Auto' | 'Suggested' | 'Manual'

// Transferência pareada: saída numa conta, entrada na outra
export interface TransferLink {
  id:        string
  out:       TransferSide
  in:        TransferSide
  origin:    TransferLinkOrigin
  score:     number | null
  createdAt: string
}

export interface TransferSuggestion {
  candidate:   TransferSide
  score:       number
  // Hoje é receita/despesa e vira transferência ao vincular
  willConvert: boolean
}

// Ponta sem par, com a melhor sugestão (se houver)
export interface PendingTransfer {
  transaction: TransferSide
  suggestion:  TransferSuggestion | null
}

export interface CreateTransferRequest {
  fromAccountId: string
  toAccountId:   string
  amount:        number
  date:          string
  description?:  string
}

export interface LinkTransfersRequest {
  firstTransactionId:  string
  secondTransactionId: string
}

export interface UnlinkTransferRequest {
  outCategoryId?:     string
  inCategoryId?:      string
  deleteTransactions: boolean
}
