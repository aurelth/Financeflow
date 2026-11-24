import type { Account, AccountReconciliation } from '@/features/accounts/types/account.types'

export const makeAccount = (overrides: Partial<Account> = {}): Account => ({
  id:                 'acc-1',
  name:               'Conta principal',
  type:               'Checking',
  bankId:             null,
  bankName:           null,
  accountLast4:       null,
  hasBankKey:         false,
  initialBalance:     0,
  initialBalanceDate: null,
  currency:           'BRL',
  color:              '#10b981',
  icon:               'landmark',
  isDefault:          true,
  isArchived:         false,
  balance:            1000,
  createdAt:          '2026-01-01T00:00:00Z',
  ...overrides,
})

export const nubank = makeAccount({
  id: 'acc-2', name: 'Nubank', isDefault: false, bankName: 'Nubank',
  accountLast4: '4321', hasBankKey: true, balance: 2500.5,
})

export const itau = makeAccount({
  id: 'acc-3', name: 'Itaú', isDefault: false, balance: -300,
})


export const makeReconciliation = (overrides: Partial<AccountReconciliation> = {}): AccountReconciliation => ({
  accountId:         'acc-1',
  status:            'Matched',
  importId:          'imp-1',
  fileName:          'fevereiro.ofx',
  statementDate:     '2026-02-28T00:00:00',
  statementStart:    '2026-02-01T00:00:00',
  statementEnd:      '2026-02-28T00:00:00',
  statementBalance:  1400,
  calculatedBalance: 1400,
  difference:        0,
  message:           null,
  ...overrides,
})
