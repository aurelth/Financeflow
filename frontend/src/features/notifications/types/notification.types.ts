export interface Notification {
  id:        string
  type:      'BudgetWarning' | 'BudgetCritical' | string
  message:   string
  isRead:    boolean
  createdAt: string
}