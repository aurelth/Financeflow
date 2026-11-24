// A API serializa enums como string (JsonStringEnumConverter)
export type AccountType = 'Checking' | 'Savings' | 'CreditCard' | 'Cash' | 'Investment'

export const ACCOUNT_TYPES: AccountType[] = [
  'Checking',
  'Savings',
  'CreditCard',
  'Cash',
  'Investment',
]

export interface Account {
  id:                 string
  name:               string
  type:               AccountType
  bankId:             string | null
  bankName:           string | null
  accountLast4:       string | null
  hasBankKey:         boolean
  initialBalance:     number
  initialBalanceDate: string | null
  currency:           string
  color:              string
  icon:               string
  isDefault:          boolean
  isArchived:         boolean
  balance:            number
  createdAt:          string
}

export interface CreateAccountRequest {
  name:               string
  type:               AccountType
  bankName:           string | null
  initialBalance:     number
  initialBalanceDate: string | null
  currency:           string | null
  color:              string | null
  icon:               string | null
}

export interface UpdateAccountRequest extends CreateAccountRequest {
  isArchived: boolean
}

// Como a conta de um arquivo OFX foi resolvida no upload
export type AccountResolution = 'Provided' | 'Recognized' | 'Learned' | 'Defaulted'

// Resultado da comparação entre o saldo do extrato (LEDGERBAL) e o saldo calculado da conta
export type ReconciliationStatus = 'NoStatement' | 'Unavailable' | 'Matched' | 'Divergent'

export interface AccountReconciliation {
  accountId:         string
  status:            ReconciliationStatus
  importId:          string | null
  fileName:          string | null
  statementDate:     string | null
  statementStart:    string | null
  statementEnd:      string | null
  statementBalance:  number | null
  calculatedBalance: number | null
  // Saldo do extrato menos o saldo calculado: positivo = faltam entradas no FinanceFlow
  difference:        number | null
  message:           string | null
}
