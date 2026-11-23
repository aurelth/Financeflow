import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import AccountSelect from '@/features/accounts/components/AccountSelect'
import type { Account } from '@/features/accounts/types/account.types'
import { makeAccount, nubank } from './fixtures'

vi.mock('react-i18next', async () => (await import('./i18nMock')).accountsI18nMock)

let mockAccounts: Account[] = []
const mockUseAccounts = vi.fn((_includeArchived?: boolean) => ({ data: mockAccounts }))

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts: (includeArchived?: boolean) => mockUseAccounts(includeArchived),
}))

const archived = makeAccount({ id: 'acc-9', name: 'Antiga', isDefault: false, isArchived: true })

describe('AccountSelect', () => {
  it('deve listar as contas e disparar onChange com o id', async () => {
    mockAccounts = [makeAccount(), nubank]
    const onChange = vi.fn()
    render(<AccountSelect value="acc-1" onChange={onChange} />)

    await userEvent.setup().selectOptions(screen.getByLabelText('Conta'), 'acc-2')
    expect(onChange).toHaveBeenCalledWith('acc-2')
  })

  it('deve oferecer "Todas as contas" como opção vazia em filtros', () => {
    mockAccounts = [makeAccount(), nubank]
    render(<AccountSelect value="" onChange={vi.fn()} emptyOption="all" />)

    expect(screen.getByRole('option', { name: 'Todas as contas' })).toHaveValue('')
  })

  it('deve oferecer "Reconhecer automaticamente" no upload de OFX', () => {
    mockAccounts = [makeAccount(), nubank]
    render(<AccountSelect value="" onChange={vi.fn()} emptyOption="auto" />)

    expect(screen.getByRole('option', { name: 'Reconhecer automaticamente' })).toBeInTheDocument()
  })

  it('não deve renderizar nada com hideWhenSingle quando só existe uma conta', () => {
    mockAccounts = [makeAccount()]
    const { container } = render(<AccountSelect value="" onChange={vi.fn()} hideWhenSingle />)

    expect(container).toBeEmptyDOMElement()
  })

  it('não deve listar contas arquivadas, exceto a que já está em uso', () => {
    mockAccounts = [makeAccount(), nubank, archived]

    const { unmount } = render(<AccountSelect value="acc-1" onChange={vi.fn()} />)
    expect(screen.queryByRole('option', { name: /Antiga/ })).not.toBeInTheDocument()
    unmount()

    render(<AccountSelect value="acc-9" onChange={vi.fn()} currentAccountId="acc-9" />)
    expect(screen.getByRole('option', { name: 'Antiga (Arquivada)' })).toBeInTheDocument()
    expect(mockUseAccounts).toHaveBeenLastCalledWith(true)
  })
})
