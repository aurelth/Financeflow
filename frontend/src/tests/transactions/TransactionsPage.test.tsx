import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import TransactionsPage from '@/features/transactions/pages/TransactionsPage'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType } from '@/features/transactions/types/transaction.types'

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useTransactions:      () => ({ data: mockPagedResult, isLoading: false }),
  useCreateTransaction: () => ({ mutate: vi.fn(), isPending: false }),
  useUpdateTransaction: () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteTransaction: () => ({ mutate: vi.fn(), isPending: false }),
  useUploadAttachment:  () => ({ mutate: vi.fn(), isPending: false }),
}))

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({ data: mockCategories, isLoading: false }),
}))

const mockCategories = [
  {
    id:            'cat-1',
    name:          'Moradia',
    icon:          'house',
    color:         '#6366f1',
    type:          TransactionType.Expense,
    isDefault:     true,
    isActive:      true,
    isOwner:       false,
    subcategories: [],
  },
  {
    id:            'cat-2',
    name:          'Salário',
    icon:          'briefcase',
    color:         '#22c55e',
    type:          TransactionType.Income,
    isDefault:     true,
    isActive:      true,
    isOwner:       false,
    subcategories: [],
  },
]

const mockTransactions = [
  {
    id:              'tx-1',
    amount:          2000,
    type:            TransactionType.Expense,
    date:            '2026-03-01T00:00:00Z',
    description:     'Aluguel',
    status:          TransactionStatus.Pending,
    isRecurring:     true,
    recurrenceType:  RecurrenceType.Monthly,
    attachmentPath:  null,
    tags:            ['moradia'],
    categoryId:      'cat-1',
    categoryName:    'Moradia',
    categoryIcon:    'house',
    categoryColor:   '#6366f1',
    subcategoryId:   null,
    subcategoryName: null,
    createdAt:       '2026-03-01T00:00:00Z',
    updatedAt:       null,
  },
  {
    id:              'tx-2',
    amount:          5000,
    type:            TransactionType.Income,
    date:            '2026-03-05T00:00:00Z',
    description:     'Salário',
    status:          TransactionStatus.Paid,
    isRecurring:     false,
    recurrenceType:  RecurrenceType.None,
    attachmentPath:  null,
    tags:            [],
    categoryId:      'cat-2',
    categoryName:    'Salário',
    categoryIcon:    'briefcase',
    categoryColor:   '#22c55e',
    subcategoryId:   null,
    subcategoryName: null,
    createdAt:       '2026-03-05T00:00:00Z',
    updatedAt:       null,
  },
]

const mockPagedResult = {
  items:      mockTransactions,
  totalCount: 2,
  page:       1,
  pageSize:   20,
  totalPages: 1,
}

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <TransactionsPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('TransactionsPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByText('Transações')).toBeInTheDocument()
  })

  it('deve exibir todas as transações', () => {
    renderPage()
    expect(screen.getByText('Aluguel')).toBeInTheDocument()
    expect(screen.getByText('Salário')).toBeInTheDocument()
  })

  it('deve exibir o badge de recorrência para transações recorrentes', () => {
    renderPage()
    expect(screen.getByText('Mensal')).toBeInTheDocument()
  })

  it('deve exibir o badge de status correto', () => {
    renderPage()
    expect(screen.getByText('Pendente')).toBeInTheDocument()
    expect(screen.getByText('Pago')).toBeInTheDocument()
  })

  it('deve exibir o nome da categoria', () => {
    renderPage()
    expect(screen.getByText('Moradia')).toBeInTheDocument()
    expect(screen.getByText('Salário')).toBeInTheDocument()
  })

  it('deve abrir modal ao clicar em nova transação', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /nova transação/i }))

    await waitFor(() => {
      expect(screen.getByText('Nova transação')).toBeInTheDocument()
    })
  })

  it('deve abrir modal de edição ao clicar em editar', async () => {
    renderPage()
    const user = userEvent.setup()

    const editButtons = screen.getAllByTitle('Editar')
    await user.click(editButtons[0])

    await waitFor(() => {
      expect(screen.getByText('Editar transação')).toBeInTheDocument()
    })
  })

  it('deve abrir modal de confirmação ao clicar em remover', async () => {
    renderPage()
    const user = userEvent.setup()

    const deleteButtons = screen.getAllByTitle('Remover')
    await user.click(deleteButtons[0])

    await waitFor(() => {
      expect(screen.getByText('Remover transação')).toBeInTheDocument()
    })
  })
})