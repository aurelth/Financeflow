import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import AccountsPage from '@/features/accounts/pages/AccountsPage'
import type { Account, AccountReconciliation } from '@/features/accounts/types/account.types'
import { makeAccount, makeReconciliation, nubank } from './fixtures'

vi.mock('react-i18next', async () => (await import('./i18nMock')).accountsI18nMock)

let mockAccounts: Account[] = []
let mockReconciliations: AccountReconciliation[] = []

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts:               () => ({ data: mockAccounts, isLoading: false }),
  useAccountsReconciliation: () => ({ data: mockReconciliations }),
  useCreateAccount:          () => ({ mutate: vi.fn(), isPending: false }),
  useUpdateAccount:          () => ({ mutate: vi.fn(), isPending: false }),
  useArchiveAccount:         () => ({ mutate: vi.fn(), isPending: false }),
  useRestoreAccount:         () => ({ mutate: vi.fn(), isPending: false }),
}))

const renderPage = () =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <AccountsPage />
    </QueryClientProvider>,
  )

describe('AccountsPage — conciliação com o extrato', () => {
  beforeEach(() => {
    mockAccounts = [makeAccount(), nubank]
    mockReconciliations = []
  })

  it('deve mostrar a situação de cada conta no próprio card', () => {
    mockReconciliations = [
      makeReconciliation({ accountId: 'acc-1', status: 'Matched' }),
      makeReconciliation({
        accountId: 'acc-2', status: 'Divergent',
        statementBalance: 1500, calculatedBalance: 1400, difference: 100,
      }),
    ]
    renderPage()

    const principal = screen.getByTestId('account-card-acc-1')
    const nu = screen.getByTestId('account-card-acc-2')

    expect(within(principal).getByTestId('reconciliation-matched')).toBeInTheDocument()
    expect(within(principal).queryByTestId('reconciliation-divergent')).not.toBeInTheDocument()
    expect(within(nu).getByTestId('reconciliation-divergent')).toHaveTextContent(/R\$\s?100,00/)
  })

  it('deve convidar a importar extrato nas contas sem saldo de extrato', () => {
    mockReconciliations = [
      makeReconciliation({ accountId: 'acc-1', status: 'NoStatement', difference: null }),
    ]
    renderPage()

    expect(within(screen.getByTestId('account-card-acc-1')).getByTestId('reconciliation-none'))
      .toHaveTextContent('Importe um OFX para conciliar')
  })

  it('não deve mostrar conciliação enquanto os dados não chegaram', () => {
    renderPage()

    expect(screen.queryByTestId('reconciliation-matched')).not.toBeInTheDocument()
    expect(screen.queryByTestId('reconciliation-divergent')).not.toBeInTheDocument()
    expect(screen.queryByTestId('reconciliation-none')).not.toBeInTheDocument()
  })

  it('não deve mostrar conciliação em conta arquivada', () => {
    mockAccounts = [makeAccount(), makeAccount({ id: 'acc-9', name: 'Antiga', isDefault: false, isArchived: true })]
    mockReconciliations = [
      makeReconciliation({ accountId: 'acc-9', status: 'Divergent', difference: 10, statementBalance: 10, calculatedBalance: 0 }),
    ]
    renderPage()

    expect(within(screen.getByTestId('account-card-acc-9')).queryByTestId('reconciliation-divergent'))
      .not.toBeInTheDocument()
  })
})
