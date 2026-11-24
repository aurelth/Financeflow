import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import TransactionForm from '@/features/transactions/components/TransactionForm'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType, type Transaction } from '@/features/transactions/types/transaction.types'
import type { Account } from '@/features/accounts/types/account.types'
import { makeAccount } from '../accounts/fixtures'

const mockMutate = vi.fn()

let mockAccounts: Account[] = []
vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts: () => ({ data: mockAccounts }),
}))

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useCreateTransaction: () => ({ mutate: mockMutate, isPending: false }),
  useUpdateTransaction: () => ({ mutate: mockMutate, isPending: false }),
  useUploadAttachment: () => ({ mutate: mockMutate, isPending: false }),
  useRemoveAttachment: () => ({ mutate: mockMutate, isPending: false }),
  getAttachmentUrl: (id: string) => `http://localhost/api/transactions/${id}/attachment`,
}))

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({
    data: [
      {
        id: 'cat-transfer',
        name: 'Transferência',
        icon: 'swap',
        color: '#818cf8',
        type: TransactionType.Transfer,
        isDefault: true,
        isActive: true,
        isOwner: false,
        isGoalCategory: false,
        subcategories: [],
      },
    ],
  }),
  useGoalCategories: () => ({ data: [] }),
}))

const transfer: Transaction = {
  id: 'tx-1',
  amount: 1000,
  type: TransactionType.Transfer,
  date: '2026-02-12T00:00:00Z',
  description: 'PIX ENVIADO',
  status: TransactionStatus.Paid,
  isRecurring: false,
  recurrenceType: RecurrenceType.None,
  recurrenceGroupId: null,
  attachmentPath: null,
  attachmentName: null,
  tags: [],
  accountId: 'acc-1',
  accountName: 'Conta principal',
  categoryId: 'cat-transfer',
  categoryName: 'Transferência',
  categoryIcon: 'swap',
  categoryColor: '#818cf8',
  subcategoryId: null,
  subcategoryName: null,
  createdAt: '2026-02-12T00:00:00Z',
  updatedAt: null,
  transferDirection: 'Out',
  transferLinkId: null,
  counterpartAccountName: null,
}

const renderForm = (transaction?: Transaction) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>
        <TransactionForm transaction={transaction} onClose={vi.fn()} />
      </MemoryRouter>
    </QueryClientProvider>,
  )

const directionGroup = () => screen.queryByRole('group', { name: 'Sentido da transferência' })

describe('TransactionForm — sentido da transferência', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockAccounts = [makeAccount()]
  })

  it('não deve mostrar o sentido em despesa/receita', () => {
    renderForm()

    expect(directionGroup()).not.toBeInTheDocument()
  })

  it('deve mostrar o sentido ao escolher Transferência, com "Saiu" como padrão', async () => {
    renderForm()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /transfer\./i }))

    expect(directionGroup()).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '↗ Saiu' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: '↙ Entrou' })).toHaveAttribute('aria-pressed', 'false')
  })

  it('deve alternar o sentido para "Entrou"', async () => {
    renderForm()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /transfer\./i }))
    await user.click(screen.getByRole('button', { name: '↙ Entrou' }))

    expect(screen.getByRole('button', { name: '↙ Entrou' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: '↗ Saiu' })).toHaveAttribute('aria-pressed', 'false')
  })

  it('deve esconder o sentido ao voltar para despesa', async () => {
    renderForm()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /transfer\./i }))
    await user.click(screen.getByRole('button', { name: '↓ Despesa' }))

    expect(directionGroup()).not.toBeInTheDocument()
  })

  it('deve carregar o sentido da transferência ao editar', () => {
    renderForm({ ...transfer, transferDirection: 'In' })

    expect(screen.getByRole('button', { name: '↙ Entrou' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('deve enviar o sentido na atualização', async () => {
    renderForm({ ...transfer, transferDirection: 'In' })
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Atualizar' }))

    expect(mockMutate).toHaveBeenCalledWith(
      expect.objectContaining({ type: TransactionType.Transfer, transferDirection: 'In' }),
      expect.any(Object),
    )
  })

  it('deve travar tipo e sentido quando a transferência está vinculada', () => {
    renderForm({ ...transfer, transferLinkId: 'link-1', counterpartAccountName: 'Conta B' })

    expect(screen.getByRole('button', { name: '↗ Saiu' })).toBeDisabled()
    expect(screen.getByRole('button', { name: '↙ Entrou' })).toBeDisabled()
    expect(screen.getByRole('button', { name: /transfer\./i })).toBeDisabled()
    expect(screen.getByRole('button', { name: '↓ Despesa' })).toBeDisabled()
    expect(screen.getByText(/Vinculada a Conta B/)).toBeInTheDocument()
  })

  it('não deve travar quando a transferência não está vinculada', () => {
    renderForm(transfer)

    expect(screen.getByRole('button', { name: '↗ Saiu' })).toBeEnabled()
    expect(screen.getByRole('button', { name: /transfer\./i })).toBeEnabled()
  })
})
