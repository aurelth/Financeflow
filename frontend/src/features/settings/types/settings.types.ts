export interface NotificationPreferences {
  budgetWarningEnabled:          boolean
  budgetCriticalEnabled:         boolean
  transactionDueTomorrowEnabled: boolean
  transactionDueIn3DaysEnabled:  boolean
}

export interface UpdateNotificationPreferencesRequest {
  budgetWarningEnabled:          boolean
  budgetCriticalEnabled:         boolean
  transactionDueTomorrowEnabled: boolean
  transactionDueIn3DaysEnabled:  boolean
}

export interface DeleteAccountRequest {
  currentPassword: string
}