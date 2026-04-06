import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import DeleteTransactionDialog from '@/features/transactions/components/DeleteTransactionDialog'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType, type Transaction } from '@/features/transactions/types/transaction.types'

const mockMutate = vi.fn()

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useDeleteTransaction: () => ({ mutate: mockMutate, isPending: false }),
}))

const mockTransaction: Transaction = {
  id:                'tx-1',
  amount:            150.00,
  type:              TransactionType.Expense,
  date:              '2026-03-10T00:00:00Z',
  description:       'Mercado',
  status:            TransactionStatus.Paid,
  isRecurring:       false,
  recurrenceType:    RecurrenceType.None,
  recurrenceGroupId: null,
  attachmentPath:    null,
  attachmentName:    null,
  tags:              [],
  categoryId:        'cat-1',
  categoryName:      'Alimentação',
  categoryIcon:      '🍔',
  categoryColor:     '#f97316',
  subcategoryId:     null,
  subcategoryName:   null,
  createdAt:         '2026-03-10T00:00:00Z',
  updatedAt:         null,
}

const mockRecurringTransaction: Transaction = {
  ...mockTransaction,
  isRecurring:       true,
  recurrenceType:    RecurrenceType.Monthly,
  recurrenceGroupId: 'group-1',
  description:       'Assinatura Mensal',
}

const renderDialog = (transaction: Transaction, onClose = vi.fn()) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <DeleteTransactionDialog transaction={transaction} onClose={onClose} />
    </QueryClientProvider>
  )
}

describe('DeleteTransactionDialog', () => {
  beforeEach(() => vi.clearAllMocks())

  // Modal padrão (não recorrente)

  it('deve exibir modal padrão para transação não recorrente', () => {
    renderDialog(mockTransaction)
    expect(screen.getByText('Remover transação')).toBeInTheDocument()
    expect(screen.getByText(/Mercado/)).toBeInTheDocument()
  })

  it('deve chamar onClose ao clicar em cancelar', async () => {
    const onClose = vi.fn()
    renderDialog(mockTransaction, onClose)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /cancelar/i }))

    expect(onClose).toHaveBeenCalledOnce()
  })

  it('deve chamar deleteTransaction com deleteFuture=false para não recorrente', async () => {
    renderDialog(mockTransaction)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /remover/i }))

    expect(mockMutate).toHaveBeenCalledWith(
      { id: 'tx-1', deleteFuture: false },
      expect.any(Object)
    )
  })

  // Modal de recorrente

  it('deve exibir modal de recorrência para transação recorrente', () => {
    renderDialog(mockRecurringTransaction)
    expect(screen.getByText('Remover transação recorrente')).toBeInTheDocument()
    expect(screen.getByText('Apenas esta')).toBeInTheDocument()
    expect(screen.getByText('Esta e todas as futuras')).toBeInTheDocument()
  })

  it('deve exibir descrição da transação no modal de recorrente', () => {
    renderDialog(mockRecurringTransaction)
    expect(screen.getByText(/Assinatura Mensal/)).toBeInTheDocument()
  })

  it('deve chamar deleteTransaction com deleteFuture=false ao clicar em "Apenas esta"', async () => {
    renderDialog(mockRecurringTransaction)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /apenas esta/i }))

    expect(mockMutate).toHaveBeenCalledWith(
      { id: 'tx-1', deleteFuture: false },
      expect.any(Object)
    )
  })

  it('deve chamar deleteTransaction com deleteFuture=true ao clicar em "Esta e todas as futuras"', async () => {
    renderDialog(mockRecurringTransaction)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /esta e todas as futuras/i }))

    expect(mockMutate).toHaveBeenCalledWith(
      { id: 'tx-1', deleteFuture: true },
      expect.any(Object)
    )
  })

  it('deve chamar onClose ao clicar em cancelar no modal de recorrente', async () => {
    const onClose = vi.fn()
    renderDialog(mockRecurringTransaction, onClose)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /cancelar/i }))

    expect(onClose).toHaveBeenCalledOnce()
  })

  it('deve fechar após deletar com sucesso', async () => {
    const onClose = vi.fn()
    const mockMutateSuccess = vi.fn((_, options) => options?.onSuccess?.())
    vi.mocked(mockMutate).mockImplementation(mockMutateSuccess)

    renderDialog(mockTransaction, onClose)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /remover/i }))

    await waitFor(() => {
      expect(onClose).toHaveBeenCalledOnce()
    })
  })
})