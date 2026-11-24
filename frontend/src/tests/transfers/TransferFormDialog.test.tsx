import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import TransferFormDialog from '@/features/transfers/components/TransferFormDialog'

const mockCreate = vi.fn()

vi.mock('@/features/transfers/api/useTransfers', () => ({
  useCreateTransfer: () => ({ mutate: mockCreate, isPending: false }),
}))

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts: () => ({
    data: [
      { id: 'acc-a', name: 'Conta A' },
      { id: 'acc-b', name: 'Conta B' },
    ],
  }),
}))

const submit = () => screen.getByRole('button', { name: 'Transferir' })

describe('TransferFormDialog', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve começar com o botão desabilitado', () => {
    render(<TransferFormDialog onClose={vi.fn()} />)
    expect(submit()).toBeDisabled()
  })

  it('deve avisar e bloquear quando origem e destino são a mesma conta', async () => {
    render(<TransferFormDialog onClose={vi.fn()} />)
    const user = userEvent.setup()

    await user.selectOptions(screen.getByLabelText('De'), 'acc-a')
    await user.selectOptions(screen.getByLabelText('Para'), 'acc-a')
    await user.type(screen.getByLabelText('Valor'), '100')

    expect(screen.getByRole('alert')).toHaveTextContent('devem ser diferentes')
    expect(submit()).toBeDisabled()
  })

  it('deve bloquear valor zero', async () => {
    render(<TransferFormDialog onClose={vi.fn()} />)
    const user = userEvent.setup()

    await user.selectOptions(screen.getByLabelText('De'), 'acc-a')
    await user.selectOptions(screen.getByLabelText('Para'), 'acc-b')
    await user.type(screen.getByLabelText('Valor'), '0')

    expect(submit()).toBeDisabled()
  })

  it('deve enviar a transferência com os dados informados', async () => {
    render(<TransferFormDialog onClose={vi.fn()} />)
    const user = userEvent.setup()

    await user.selectOptions(screen.getByLabelText('De'), 'acc-a')
    await user.selectOptions(screen.getByLabelText('Para'), 'acc-b')
    await user.type(screen.getByLabelText('Valor'), '250.50')
    await user.type(screen.getByLabelText(/Descrição/), '  Reserva  ')

    await user.click(submit())

    expect(mockCreate).toHaveBeenCalledWith(
      expect.objectContaining({
        fromAccountId: 'acc-a',
        toAccountId: 'acc-b',
        amount: 250.5,
        description: 'Reserva',
        date: expect.stringMatching(/^\d{4}-\d{2}-\d{2}$/),
      }),
      expect.any(Object),
    )
  })

  it('deve omitir a descrição quando em branco', async () => {
    render(<TransferFormDialog onClose={vi.fn()} />)
    const user = userEvent.setup()

    await user.selectOptions(screen.getByLabelText('De'), 'acc-b')
    await user.selectOptions(screen.getByLabelText('Para'), 'acc-a')
    await user.type(screen.getByLabelText('Valor'), '10')
    await user.click(submit())

    expect(mockCreate.mock.calls[0][0].description).toBeUndefined()
  })

  it('deve fechar ao cancelar', async () => {
    const onClose = vi.fn()
    render(<TransferFormDialog onClose={onClose} />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(onClose).toHaveBeenCalled()
  })
})
