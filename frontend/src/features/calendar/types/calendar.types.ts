import type { TransactionType } from '../../categories/types/category.types'
import type { TransactionStatus } from '../../transactions/types/transaction.types'

// Espelha CalendarDtos.cs. Enums chegam como string: use resolveType/resolveStatus de '@/lib/enumUtils'.

export interface CalendarEntry {
  id:                string
  description:       string
  amount:            number
  type:              TransactionType | string
  status:            TransactionStatus | string
  date:              string
  isRecurring:       boolean
  installmentNumber: number | null
  installmentCount:  number | null
  categoryId:        string
  categoryName:      string
  categoryIcon:      string
  categoryColor:     string
  accountId:         string
  accountName:       string
  isOverdue:         boolean
}

// "Em aberto" são Pendentes/Agendados; "liquidado" são os Pagos
export interface CalendarTotals {
  toPay:     number
  toReceive: number
  paid:      number
  received:  number
}

export interface CalendarDay {
  date:    string
  totals:  CalendarTotals
  entries: CalendarEntry[]
}

export interface CalendarOverdue {
  count:         number
  expenseAmount: number
  incomeAmount:  number
  entries:       CalendarEntry[]
}

export interface CalendarMonth {
  month:   number
  year:    number
  today:   string
  days:    CalendarDay[]
  totals:  CalendarTotals
  overdue: CalendarOverdue
}
