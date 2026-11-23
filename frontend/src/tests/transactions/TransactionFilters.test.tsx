import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import TransactionFilters from '@/features/transactions/components/TransactionFilters'
import type { Account } from '@/features/accounts/types/account.types'
import { makeAccount, nubank } from '../accounts/fixtures'

let mockAccounts: Account[] = []
vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts: () => ({ data: mockAccounts }),
}))

const renderFilters = (filters = {}, onChange = vi.fn()) =>
  render(
    <TransactionFilters filters={filters} categories={[]} onChange={onChange} onClear={vi.fn()} />,
  )

describe('TransactionFilters — filtro por conta', () => {
  it('não deve exibir o filtro de conta quando só existe uma conta', () => {
    mockAccounts = [makeAccount()]
    renderFilters()
    expect(screen.queryByRole('option', { name: 'select.allAccounts' })).not.toBeInTheDocument()
  })

  it('deve filtrar pela conta escolhida preservando os outros filtros', async () => {
    mockAccounts = [makeAccount(), nubank]
    const onChange = vi.fn()
    renderFilters({ page: 1, search: 'mercado' }, onChange)

    await userEvent.setup().selectOptions(screen.getByLabelText('select.label'), 'acc-2')

    expect(onChange).toHaveBeenCalledWith({ page: 1, search: 'mercado', accountId: 'acc-2' })
  })

  it('deve remover o filtro ao voltar para "Todas as contas"', async () => {
    mockAccounts = [makeAccount(), nubank]
    const onChange = vi.fn()
    renderFilters({ accountId: 'acc-2' }, onChange)

    await userEvent.setup().selectOptions(screen.getByLabelText('select.label'), '')

    expect(onChange).toHaveBeenCalledWith({ accountId: undefined })
  })
})
