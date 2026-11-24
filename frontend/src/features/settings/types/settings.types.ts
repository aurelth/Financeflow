export interface NotificationPreferences {
  budgetWarningEnabled:          boolean
  budgetCriticalEnabled:         boolean
  transactionDueTomorrowEnabled: boolean
  transactionDueIn3DaysEnabled:  boolean
  transactionDueTodayEnabled:    boolean
  transactionOverdueEnabled:     boolean
}

export interface UpdateNotificationPreferencesRequest {
  budgetWarningEnabled:          boolean
  budgetCriticalEnabled:         boolean
  transactionDueTomorrowEnabled: boolean
  transactionDueIn3DaysEnabled:  boolean
  transactionDueTodayEnabled:    boolean
  transactionOverdueEnabled:     boolean
}

export interface DeleteAccountRequest {
  currentPassword: string
}

export interface CategoryRule {
  id:           string
  // Descrição normalizada que dispara a regra (ex.: "mercado extra")
  key:          string
  type:         'Income' | 'Expense'
  categoryId:   string
  categoryName: string
  categoryIcon: string
  categoryColor: string
  // Quantas vezes o usuário confirmou esta mesma categoria
  hitCount:     number
  lastUsedAt:   string
}
