export type BankImportStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed'

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