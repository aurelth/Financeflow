import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import PendingTransfersPanel from '@/features/transfers/components/PendingTransfersPanel'
import type { PendingTransfer } from '@/features/transfers/types/transfer.types'

const mockLink = vi.fn()
let mockPending: PendingTransfer[] = []
let mockLoading = false

vi.mock('@/features/transfers/api/useTransfers', () => ({
  usePendingTransfers: () => ({ data: mockPending, isLoading: mockLoading }),
  useLinkTransfers: () => ({ mutate: mockLink, isPending: false }),
}))

const side = (overrides: Partial<PendingTransfer['transaction']> = {}) => ({
  transactionId: 'tx-a',
  accountId: 'acc-a',
  accountName: 'Conta A',
  date: '2026-02-12T00:00:00',
  amount: 1000,
  description: 'PIX ENVIADO',
  type: 'Transfer',
  direction: 'Out' as const,
  ...overrides,
})

describe('PendingTransfersPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockPending = []
    mockLoading = false
  })

  it('não deve renderizar nada quando não há pendências', () => {
    const { container } = render(<PendingTransfersPanel />)
    expect(container).toBeEmptyDOMElement()
  })

  it('não deve renderizar nada enquanto carrega', () => {
    mockLoading = true
    mockPending = [{ transaction: side(), suggestion: null }]
    const { container } = render(<PendingTransfersPanel />)
    expect(container).toBeEmptyDOMElement()
  })

  it('deve mostrar a contagem e os dados da ponta sem par', () => {
    mockPending = [{ transaction: side(), suggestion: null }]
    render(<PendingTransfersPanel />)

    expect(screen.getByText('1 transferência sem par')).toBeInTheDocument()
    expect(screen.getByText('PIX ENVIADO')).toBeInTheDocument()
    expect(screen.getByText(/Saída · Conta A/)).toBeInTheDocument()
    expect(screen.getByText(/Sem sugestão/)).toBeInTheDocument()
  })

  it('deve usar o plural quando há mais de uma pendência', () => {
    mockPending = [
      { transaction: side(), suggestion: null },
      { transaction: side({ transactionId: 'tx-b' }), suggestion: null },
    ]
    render(<PendingTransfersPanel />)

    expect(screen.getByText('2 transferências sem par')).toBeInTheDocument()
  })

  it('deve mostrar a sugestão e vincular com um clique', async () => {
    mockPending = [{
      transaction: side(),
      suggestion: {
        candidate: side({
          transactionId: 'tx-b',
          accountId: 'acc-b',
          accountName: 'Conta B',
          description: 'PIX RECEBIDO',
          type: 'Income',
          direction: null,
        }),
        score: 80,
        willConvert: true,
      },
    }]
    render(<PendingTransfersPanel />)
    const user = userEvent.setup()

    expect(screen.getByText(/Par provável: Conta B/)).toBeInTheDocument()
    expect(screen.getByText(/vira transferência/)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Vincular PIX ENVIADO' }))

    expect(mockLink).toHaveBeenCalledWith({
      firstTransactionId: 'tx-a',
      secondTransactionId: 'tx-b',
    })
  })

  it('deve listar primeiro quem tem sugestão quando a API já envia ordenado', () => {
    mockPending = [
      {
        transaction: side({ transactionId: 'tx-1', description: 'COM PAR' }),
        suggestion: { candidate: side({ transactionId: 'tx-2', accountName: 'Conta B' }), score: 80, willConvert: false },
      },
      { transaction: side({ transactionId: 'tx-3', description: 'SEM PAR' }), suggestion: null },
    ]
    render(<PendingTransfersPanel />)

    const items = screen.getAllByRole('listitem')
    expect(items[0]).toHaveTextContent('COM PAR')
    expect(items[1]).toHaveTextContent('SEM PAR')
  })
})
