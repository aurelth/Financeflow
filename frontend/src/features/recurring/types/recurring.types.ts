import type { TransactionType } from '../../categories/types/category.types'
import type { RecurrenceType } from '../../transactions/types/transaction.types'

// Espelha RecurringDtos.cs. Enums chegam como string neste endpoint: use resolveType/resolveRecurrence.

export type RecurringKind = 'Subscription' | 'Bill' | 'Income'

export interface RecurringSuggestion {
  key:              string
  type:             TransactionType | string
  description:      string
  cadence:          RecurrenceType | string
  kind:             RecurringKind | string
  amount:           number
  amountVaries:     boolean
  occurrences:      number
  firstDate:        string
  lastDate:         string
  nextExpectedDate: string
  monthlyCost:      number
  confidence:       number
  categoryId:       string
  categoryName:     string
  categoryIcon:     string
  categoryColor:    string
  accountId:        string
  accountName:      string
}

export interface RecurringSuggestions {
  items:               RecurringSuggestion[]
  monthlyExpenseTotal: number
  monthlyIncomeTotal:  number
}

// Identifica a sugestão: devolve exatamente o que o servidor mandou
export interface RecurringSuggestionRef {
  type: TransactionType | string
  key:  string
}

export interface ConfirmRecurringResult {
  recurrenceGroupId: string
  cadence:           RecurrenceType | string
  linkedCount:       number
  projectedCount:    number
  nextProjectedDate: string | null
}
