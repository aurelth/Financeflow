export type BankImportStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed'

import type { AccountResolution } from '@/features/accounts/types/account.types'

export type TransactionType = 'Income' | 'Expense'

export interface BankImportDto {
  id: string
  fileName: string
  status: BankImportStatus
  totalRecords: number
  imported: number
  duplicates: number
  errors: number
  errorMessage: string | null
  createdAt: string
  accountId?: string
  accountName?: string | null
  // Só vem preenchido na resposta do upload
  accountResolution?: AccountResolution | null
}

export interface BankImportTransactionDto {
  id: string
  externalId: string
  date: string
  amount: number
  description: string
  type: TransactionType
  hash: string
  suggestedCategoryId: string | null
  isDuplicate: boolean
  isSelected: boolean
  transactionId: string | null
}

export interface BankImportPreviewDto {
  importId: string
  fileName: string
  status: BankImportStatus
  totalRecords: number
  transactions: BankImportTransactionDto[]
}

export interface ConfirmImportItemDto {
  id:         string
  isSelected: boolean
  categoryId: string
}

export interface ConfirmImportRequestDto {
  transactions: ConfirmImportItemDto[]
}