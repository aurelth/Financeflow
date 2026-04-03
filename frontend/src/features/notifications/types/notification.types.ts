export interface Notification {
  id:        string
  type:      'BudgetWarning' | 'BudgetCritical'  | 'TransactionDueIn3Days' | string
  message:   string
  isRead:    boolean
  referenceId?: string | null
  createdAt: string
}