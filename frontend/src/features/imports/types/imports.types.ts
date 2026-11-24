import type { AccountResolution } from '@/features/accounts/types/account.types'

export type BankImportStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed'

export type TransactionType = 'Income' | 'Expense'

// Por que uma linha do extrato foi marcada como duplicada
export type DuplicateReason = 'SameFitId' | 'SameContent' | 'PossibleManual'

// Confiança do pareamento com um lançamento de outra conta
export type TransferMatchStatus = 'Auto' | 'Suggested'

// Lançamento de outra conta que parece ser a outra ponta da transferência
export interface TransferMatchDto {
  transactionId: string
  accountId?: string | null
  accountName?: string | null
  date: string
  amount: number
  description: string
  score: number
  status: TransferMatchStatus
  // Hoje é receita/despesa e vira transferência ao vincular
  willConvert: boolean
}

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

// Lançamento já existente com o qual a linha foi confundida
export interface DuplicateMatchDto {
  transactionId: string
  date: string
  amount: number
  description: string
}

export interface BankImportTransactionDto {
  id: string
  externalId: string
  date: string
  amount: number
  description: string
  type: TransactionType
  hash?: string
  suggestedCategoryId: string | null
  suggestedCategoryName?: string | null
  isDuplicate: boolean
  isSelected: boolean
  transactionId?: string | null
  duplicateReason?: DuplicateReason | null
  match?: DuplicateMatchDto | null
  transferMatch?: TransferMatchDto | null
}

export interface BankImportPreviewDto {
  importId: string
  fileName: string
  status: BankImportStatus
  totalRecords: number
  duplicates?: number
  autoTransfers?: number
  suggestedTransfers?: number
  accountId?: string | null
  accountName?: string | null
  transactions: BankImportTransactionDto[]
}

export interface ConfirmImportItemDto {
  id:         string
  isSelected: boolean
  categoryId: string
  type?:      string
  // Só enviado para linhas duplicadas que o usuário decidiu importar mesmo assim
  importAnyway?: boolean
  // Só enviado para linhas com par de transferência: vincular as duas pontas ou ignorar o par
  transferAction?: 'Link' | 'Ignore'
}

export interface ConfirmImportRequestDto {
  transactions: ConfirmImportItemDto[]
}

// Códigos de erro por linha devolvidos pela API quando a confirmação é bloqueada (HTTP 422)
export type ImportRowError = 'MissingCategory' | 'InvalidCategory' | 'UnknownTransferDirection'
