export interface Notification {
  id:        string
  type:      'BudgetWarning' | 'BudgetCritical'  | 'TransactionDueIn3Days' | 'TransactionDueToday' | 'TransactionOverdue' | 'WeeklySummary' | string
  message:   string
  isRead:    boolean
  referenceId?: string | null
  createdAt: string
}