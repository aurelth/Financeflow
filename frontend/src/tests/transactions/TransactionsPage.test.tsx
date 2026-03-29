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
  useRemoveAttachment:  () => ({ mutate: vi.fn(), isPending: false }),
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
    name:          'Trabalho',
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
    description:     'Aluguel Mensal',
    status:          TransactionStatus.Pending,
    isRecurring:     true,
    recurrenceType:  RecurrenceType.Monthly,
    attachmentPath:  null,
    attachmentName:  null,
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
    description:     'Pagamento Freelance',
    status:          TransactionStatus.Paid,
    isRecurring:     false,
    recurrenceType:  RecurrenceType.None,
    attachmentPath:  null,
    attachmentName:  null,
    tags:            [],
    categoryId:      'cat-2',
    categoryName:    'Trabalho',
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
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Transações')
  })

  it('deve exibir todas as transações', () => {
    renderPage()
    expect(screen.getByText('Aluguel Mensal')).toBeInTheDocument()
    expect(screen.getByText('Pagamento Freelance')).toBeInTheDocument()
  })

  it('deve exibir o badge de recorrência para transações recorrentes', () => {
    renderPage()
    expect(screen.getByText('Mensal')).toBeInTheDocument()
  })

  it('deve exibir o badge de status correto', () => {
    renderPage()
    expect(screen.getAllByText('Pendente').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Pago').length).toBeGreaterThan(0)
  })

  it('deve exibir o nome da categoria', () => {
    renderPage()
    expect(screen.getAllByText('Moradia').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Trabalho').length).toBeGreaterThan(0)
  })

  it('deve abrir modal ao clicar em nova transação', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /nova transação/i }))

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Nova transação' })).toBeInTheDocument()
    })
  })

  it('deve abrir modal de edição ao clicar em editar', async () => {
    renderPage()
    const user = userEvent.setup()

    const editButtons = screen.getAllByTitle('Editar')
    await user.click(editButtons[0])

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Editar transação' })).toBeInTheDocument()
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

  it('deve iniciar com filtro do mês atual por defeito', () => {
  renderPage()

  const now      = new Date()
  const year     = now.getFullYear()
  const month    = now.getMonth()
  const firstDay = new Date(year, month, 1).toISOString().split('T')[0]
  const lastDay  = new Date(year, month + 1, 0).toISOString().split('T')[0]

  // Verifica que os inputs de data têm os valores do mês atual
  const dateInputs = screen.getAllByDisplayValue(firstDay)
  expect(dateInputs.length).toBeGreaterThan(0)

  const dateInputsEnd = screen.getAllByDisplayValue(lastDay)
  expect(dateInputsEnd.length).toBeGreaterThan(0)
 })
})