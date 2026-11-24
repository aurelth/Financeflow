import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import DeleteTransactionDialog from '@/features/transactions/components/DeleteTransactionDialog'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType, type Transaction } from '@/features/transactions/types/transaction.types'

const mockMutate = vi.fn()

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useDeleteTransaction: () => ({ mutate: mockMutate, isPending: false }),
}))

const parcel: Transaction = {
  id: 'tx-p2', amount: 100, type: TransactionType.Expense, date: '2026-12-24T00:00:00Z',
  description: 'Notebook', status: TransactionStatus.Scheduled, isRecurring: false,
  recurrenceType: RecurrenceType.None, recurrenceGroupId: null, attachmentPath: null, attachmentName: null,
  tags: [], accountId: 'acc-1', accountName: 'Conta principal', categoryId: 'cat-1', categoryName: 'Eletrônicos',
  categoryIcon: '💻', categoryColor: '#6366f1', subcategoryId: null, subcategoryName: null,
  createdAt: '2026-11-24T00:00:00Z', updatedAt: null,
  installmentGroupId: 'grp-1', installmentNumber: 2, installmentCount: 12,
}

const renderDialog = (onClose = vi.fn()) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <DeleteTransactionDialog transaction={parcel} onClose={onClose} />
    </QueryClientProvider>,
  )

describe('DeleteTransactionDialog — parcela', () => {
  beforeEach(() => vi.clearAllMocks())

  it('oferece as opções de parcela, não as de recorrência', () => {
    renderDialog()

    expect(screen.getByText('installments.deleteTitle')).toBeInTheDocument()
    expect(screen.getByText('installments.deleteOnly')).toBeInTheDocument()
    expect(screen.getByText('installments.deleteFuture')).toBeInTheDocument()
    expect(screen.queryByText('Remover transação recorrente')).not.toBeInTheDocument()
    expect(screen.queryByText('Esta e todas as futuras')).not.toBeInTheDocument()
  })

  it('"apenas esta parcela" remove só ela (deleteFuture=false)', async () => {
    renderDialog()

    await userEvent.setup().click(screen.getByRole('button', { name: /installments\.deleteOnly/ }))

    expect(mockMutate).toHaveBeenCalledWith({ id: 'tx-p2', deleteFuture: false }, expect.any(Object))
  })

  it('"esta e as seguintes" remove também as seguintes (deleteFuture=true)', async () => {
    renderDialog()

    await userEvent.setup().click(screen.getByRole('button', { name: /installments\.deleteFuture/ }))

    expect(mockMutate).toHaveBeenCalledWith({ id: 'tx-p2', deleteFuture: true }, expect.any(Object))
  })
})
