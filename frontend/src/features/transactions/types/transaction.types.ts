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
  attachmentPath: string | null
  attachmentName:  string | null
  tags:           string[]
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
}

export interface GetTransactionsQuery {
  page?:          number
  pageSize?:      number
  dateFrom?:      string
  dateTo?:        string
  categoryId?:    string
  subcategoryId?: string
  type?:          TransactionType
  status?:        TransactionStatus
  amountMin?:     number
  amountMax?:     number
  search?:        string
}