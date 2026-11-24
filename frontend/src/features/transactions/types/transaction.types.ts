import { TransactionType } from '../../categories/types/category.types'

// Enums

export enum TransactionStatus {
  Paid      = 1,
  Pending   = 2,
  Scheduled = 3,
}

export enum RecurrenceType {
  None    = 0,
  Daily   = 1,
  Weekly  = 2,
  Monthly = 3,
  Yearly  = 4,
}

// Sentido de uma ponta de transferência, do ponto de vista da conta do lançamento
export type TransferDirection = 'In' | 'Out'

// Transação

export interface Transaction {
  id:             string
  amount:         number
  type:           TransactionType
  date:           string
  description:    string
  status:         TransactionStatus
  isRecurring:    boolean
  recurrenceType: RecurrenceType
  recurrenceGroupId: string | null
  // Compra parcelada (ausentes/nulos quando o lançamento não é parcelado)
  installmentGroupId?: string | null
  installmentNumber?:  number | null
  installmentCount?:   number | null
  attachmentPath: string | null
  attachmentName:  string | null
  tags:           string[]
  accountId:      string
  accountName:    string | null
  // Transferência entre contas (só quando type = Transfer)
  transferDirection?:        TransferDirection | null
  transferLinkId?:           string | null
  counterpartTransactionId?: string | null
  counterpartAccountId?:     string | null
  counterpartAccountName?:   string | null
  categoryId:     string
  categoryName:   string
  categoryIcon:   string
  categoryColor:  string
  subcategoryId:  string | null
  subcategoryName: string | null
  createdAt:      string
  updatedAt:      string | null
}

// Paginação

export interface PagedResult<T> {
  items:      T[]
  totalCount: number
  page:       number
  pageSize:   number
  totalPages: number
}

// Requests

export interface CreateTransactionRequest {
  amount:         number
  type:           TransactionType
  date:           string
  description:    string
  status:         TransactionStatus
  isRecurring:    boolean
  recurrenceType: RecurrenceType
  categoryId:     string
  subcategoryId:  string | null
  tags:           string[]
  accountId?:     string
  // Obrigatório quando type = Transfer: a conta recebe (In) ou envia (Out) o dinheiro
  transferDirection?: TransferDirection | null
  // Compra parcelada: `amount` é o TOTAL, dividido em N parcelas mensais (2 a 60)
  installmentCount?: number | null
}

export interface UpdateTransactionRequest {
  amount:         number
  type:           TransactionType
  date:           string
  description:    string
  status:         TransactionStatus
  isRecurring:    boolean
  recurrenceType: RecurrenceType
  categoryId:     string
  subcategoryId:  string | null
  tags:           string[]
  accountId?:     string
  transferDirection?: TransferDirection | null
  propagateToFuture: boolean
}

export interface GetTransactionsQuery {
  page?:          number
  pageSize?:      number
  dateFrom?:      string
  dateTo?:        string
  categoryId?:    string
  subcategoryId?: string
  accountId?:     string
  type?:          TransactionType
  status?:        TransactionStatus
  amountMin?:     number
  amountMax?:     number
  search?:        string
}