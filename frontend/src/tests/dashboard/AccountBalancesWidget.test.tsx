import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import AccountBalancesWidget from '@/features/dashboard/components/AccountBalancesWidget'
import type { Account, AccountReconciliation } from '@/features/accounts/types/account.types'
import { makeAccount, makeReconciliation, nubank, itau } from '../accounts/fixtures'

vi.mock('react-i18next', async () => (await import('../accounts/i18nMock')).accountsI18nMock)

let mockAccounts: Account[] = []
let mockReconciliations: AccountReconciliation[] = []

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts:               () => ({ data: mockAccounts }),
  useAccountsReconciliation: () => ({ data: mockReconciliations }),
}))

const renderWidget = () =>
  render(
    <MemoryRouter>
      <AccountBalancesWidget />
    </MemoryRouter>,
  )

describe('AccountBalancesWidget', () => {
  beforeEach(() => {
    mockAccounts = [makeAccount(), nubank, itau]
    mockReconciliations = []
  })

  it('deve listar o saldo atual de cada conta ativa', () => {
    renderWidget()

    expect(screen.getByRole('heading', { name: 'Saldo por conta' })).toBeInTheDocument()
    expect(within(screen.getByTestId('account-balance-acc-1')).getByText(/R\$\s?1\.000,00/)).toBeInTheDocument()
    expect(within(screen.getByTestId('account-balance-acc-2')).getByText(/R\$\s?2\.500,50/)).toBeInTheDocument()
    const negative = within(screen.getByTestId('account-balance-acc-3')).getByText(/-R\$\s?300,00|-\s?R\$\s?300,00/)
    expect(negative).toHaveStyle({ color: 'var(--ff-expense)' })
  })

  it('não deve listar contas arquivadas', () => {
    mockAccounts = [makeAccount(), nubank, makeAccount({ id: 'acc-9', name: 'Antiga', isDefault: false, isArchived: true })]
    renderWidget()

    expect(screen.queryByTestId('account-balance-acc-9')).not.toBeInTheDocument()
  })

  it('não deve aparecer com uma única conta sem extrato com saldo', () => {
    mockAccounts = [makeAccount()]
    mockReconciliations = [makeReconciliation({ accountId: 'acc-1', status: 'NoStatement', difference: null })]
    const { container } = renderWidget()

    expect(container).toBeEmptyDOMElement()
  })

  it('deve aparecer com uma única conta que já tem extrato conciliado', () => {
    mockAccounts = [makeAccount()]
    mockReconciliations = [makeReconciliation({ accountId: 'acc-1', status: 'Matched' })]
    renderWidget()

    expect(screen.getByTestId('account-balances-widget')).toBeInTheDocument()
    expect(screen.getByTestId('account-balance-acc-1')).toHaveTextContent('Conciliação com o extrato')
  })

  it('deve destacar a diferença com o extrato da conta divergente', () => {
    mockReconciliations = [
      makeReconciliation({ accountId: 'acc-1', status: 'Matched' }),
      makeReconciliation({
        accountId: 'acc-2', status: 'Divergent',
        statementBalance: 1500, calculatedBalance: 1400, difference: 100,
      }),
    ]
    renderWidget()

    expect(screen.getByTestId('account-balance-acc-2')).toHaveTextContent(/Diferença com o extrato:\s?R\$\s?100,00/)
    expect(screen.getByTestId('account-balance-acc-1')).not.toHaveTextContent('Diferença com o extrato')
  })

  it('deve levar para a página de contas', () => {
    renderWidget()

    expect(screen.getByRole('link', { name: 'Ver contas' })).toHaveAttribute('href', '/accounts')
  })
})
