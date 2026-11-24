import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import UnlinkTransferDialog from '@/features/transfers/components/UnlinkTransferDialog'

const mockUnlink = vi.fn()

vi.mock('@/features/transfers/api/useTransfers', () => ({
  useUnlinkTransfer: () => ({ mutate: mockUnlink, isPending: false }),
}))

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({
    data: [
      { id: 'exp-1', name: 'Mercado', type: 2 },
      { id: 'inc-1', name: 'Salário', type: 1 },
      { id: 'tr-1', name: 'Transferência', type: 3 },
    ],
  }),
}))

describe('UnlinkTransferDialog', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve mostrar o resumo do par', () => {
    render(<UnlinkTransferDialog linkId="link-1" summary="Pix · Conta A ↔ Conta B" onClose={vi.fn()} />)

    expect(screen.getByText('Pix · Conta A ↔ Conta B')).toBeInTheDocument()
  })

  it('deve oferecer só categorias de despesa para a saída e de receita para a entrada', () => {
    render(<UnlinkTransferDialog linkId="link-1" onClose={vi.fn()} />)

    const out = screen.getByLabelText(/Categoria da saída/)
    const inn = screen.getByLabelText(/Categoria da entrada/)

    expect(out).toHaveTextContent('Mercado')
    expect(out).not.toHaveTextContent('Salário')
    expect(out).not.toHaveTextContent('Transferência')
    expect(inn).toHaveTextContent('Salário')
    expect(inn).not.toHaveTextContent('Mercado')
  })

  it('deve manter "Desfazer vínculo" desabilitado até escolher as duas categorias', async () => {
    render(<UnlinkTransferDialog linkId="link-1" onClose={vi.fn()} />)
    const user = userEvent.setup()
    const revert = screen.getByRole('button', { name: 'Desfazer vínculo' })

    expect(revert).toBeDisabled()

    await user.selectOptions(screen.getByLabelText(/Categoria da saída/), 'exp-1')
    expect(revert).toBeDisabled()

    await user.selectOptions(screen.getByLabelText(/Categoria da entrada/), 'inc-1')
    expect(revert).toBeEnabled()
  })

  it('deve devolver as pontas como despesa e receita com as categorias escolhidas', async () => {
    render(<UnlinkTransferDialog linkId="link-1" onClose={vi.fn()} />)
    const user = userEvent.setup()

    await user.selectOptions(screen.getByLabelText(/Categoria da saída/), 'exp-1')
    await user.selectOptions(screen.getByLabelText(/Categoria da entrada/), 'inc-1')
    await user.click(screen.getByRole('button', { name: 'Desfazer vínculo' }))

    expect(mockUnlink).toHaveBeenCalledWith(
      { outCategoryId: 'exp-1', inCategoryId: 'inc-1', deleteTransactions: false },
      expect.any(Object),
    )
  })

  it('deve permitir excluir os dois lançamentos sem escolher categoria', async () => {
    render(<UnlinkTransferDialog linkId="link-1" onClose={vi.fn()} />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Excluir os dois lançamentos' }))

    expect(mockUnlink).toHaveBeenCalledWith({ deleteTransactions: true }, expect.any(Object))
  })
})
