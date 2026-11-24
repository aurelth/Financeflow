import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import ReconciliationAlert from '@/features/accounts/components/ReconciliationAlert'
import type { Account, AccountReconciliation } from '@/features/accounts/types/account.types'
import { makeAccount, makeReconciliation, nubank, itau } from './fixtures'

vi.mock('react-i18next', async () => (await import('./i18nMock')).accountsI18nMock)

let mockAccounts: Account[] = []
let mockReconciliations: AccountReconciliation[] = []

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts:               () => ({ data: mockAccounts }),
  useAccountsReconciliation: () => ({ data: mockReconciliations }),
}))

const renderAlert = () =>
  render(
    <MemoryRouter>
      <ReconciliationAlert />
    </MemoryRouter>,
  )

const divergent = (accountId: string, difference: number) =>
  makeReconciliation({ accountId, status: 'Divergent', difference, statementBalance: 100, calculatedBalance: 100 - difference })

describe('ReconciliationAlert', () => {
  beforeEach(() => {
    mockAccounts = [makeAccount(), nubank, itau]
    mockReconciliations = []
  })

  it('não deve aparecer quando nenhuma conta tem diferença', () => {
    mockReconciliations = [
      makeReconciliation({ accountId: 'acc-1', status: 'Matched' }),
      makeReconciliation({ accountId: 'acc-2', status: 'NoStatement', difference: null }),
    ]
    const { container } = renderAlert()

    expect(container).toBeEmptyDOMElement()
  })

  it('deve avisar a conta com diferença e o valor', () => {
    mockReconciliations = [divergent('acc-2', 100)]
    renderAlert()

    const alert = screen.getByRole('alert')
    expect(alert).toHaveTextContent('1 conta com diferença em relação ao extrato')
    expect(alert).toHaveTextContent(/Nubank: R\$\s?100,00/)
  })

  it('deve usar o plural e listar todas as contas divergentes', () => {
    mockReconciliations = [divergent('acc-2', 100), divergent('acc-3', -50.5)]
    renderAlert()

    const alert = screen.getByRole('alert')
    expect(alert).toHaveTextContent('2 contas com diferença em relação ao extrato')
    expect(alert).toHaveTextContent(/Nubank: R\$\s?100,00/)
    // A diferença aparece sempre em valor absoluto
    expect(alert).toHaveTextContent(/Itaú: R\$\s?50,50/)
  })

  it('deve levar para a página de contas', () => {
    mockReconciliations = [divergent('acc-2', 100)]
    renderAlert()

    expect(screen.getByRole('link', { name: 'Ver contas' })).toHaveAttribute('href', '/accounts')
  })

  it('deve ignorar conciliação de conta que não está mais na lista (arquivada)', () => {
    mockReconciliations = [divergent('acc-arquivada', 100)]
    const { container } = renderAlert()

    expect(container).toBeEmptyDOMElement()
  })
})
