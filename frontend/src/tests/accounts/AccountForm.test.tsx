import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { toast } from 'sonner'
import AccountForm from '@/features/accounts/components/AccountForm'
import { nubank } from './fixtures'

vi.mock('react-i18next', async () => (await import('./i18nMock')).accountsI18nMock)
vi.mock('sonner', () => ({ toast: { error: vi.fn(), success: vi.fn() } }))

const mockCreate = vi.fn()
const mockUpdate = vi.fn()

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useCreateAccount: () => ({ mutate: mockCreate, isPending: false }),
  useUpdateAccount: () => ({ mutate: mockUpdate, isPending: false }),
}))

describe('AccountForm', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve exigir o nome da conta', async () => {
    render(<AccountForm onClose={vi.fn()} />)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Salvar' }))

    expect(toast.error).toHaveBeenCalledWith('Informe o nome da conta.')
    expect(mockCreate).not.toHaveBeenCalled()
  })

  it('deve criar a conta com os dados informados e valores nulos nos opcionais vazios', async () => {
    render(<AccountForm onClose={vi.fn()} />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Nome'), '  Itaú  ')
    await user.selectOptions(screen.getByLabelText('Tipo'), 'Savings')
    await user.clear(screen.getByLabelText('Saldo inicial'))
    await user.type(screen.getByLabelText('Saldo inicial'), '150.5')
    await user.click(screen.getByRole('button', { name: 'Salvar' }))

    expect(mockCreate).toHaveBeenCalledWith(
      {
        name: 'Itaú',
        type: 'Savings',
        bankName: null,
        initialBalance: 150.5,
        initialBalanceDate: null,
        currency: null,
        color: '#10b981',
        icon: null,
      },
      expect.any(Object),
    )
  })

  it('deve atualizar mantendo moeda, ícone e estado de arquivamento da conta', async () => {
    render(<AccountForm account={nubank} onClose={vi.fn()} />)
    const user = userEvent.setup()

    expect(screen.getByLabelText('Nome')).toHaveValue('Nubank')
    await user.clear(screen.getByLabelText('Nome'))
    await user.type(screen.getByLabelText('Nome'), 'Nubank Roxinho')
    await user.click(screen.getByRole('button', { name: 'Salvar' }))

    expect(mockUpdate).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Nubank Roxinho',
        bankName: 'Nubank',
        currency: 'BRL',
        icon: 'landmark',
        isArchived: false,
      }),
      expect.any(Object),
    )
    expect(mockCreate).not.toHaveBeenCalled()
  })

  it('deve fechar ao cancelar', async () => {
    const onClose = vi.fn()
    render(<AccountForm onClose={onClose} />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Cancelar' }))
    expect(onClose).toHaveBeenCalled()
  })
})
