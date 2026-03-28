import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import TransactionTable from '@/features/transactions/components/TransactionTable'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType } from '@/features/transactions/types/transaction.types'

const mockTransactions = [
  {
    id:              'tx-1',
    amount:          150.00,
    type:            TransactionType.Expense,
    date:            '2026-03-10T00:00:00Z',
    description:     'Mercado',
    status:          TransactionStatus.Paid,
    isRecurring:     false,
    recurrenceType:  RecurrenceType.None,
    attachmentPath:  null,
    tags:            ['alimentação'],
    categoryId:      'cat-1',
    categoryName:    'Alimentação',
    categoryIcon:    '🍔',
    categoryColor:   '#f97316',
    subcategoryId:   null,
    subcategoryName: null,
    createdAt:       '2026-03-10T00:00:00Z',
    updatedAt:       null,
  },
  {
    id:              'tx-2',
    amount:          3000.00,
    type:            TransactionType.Income,
    date:            '2026-03-05T00:00:00Z',
    description:     'Freelance',
    status:          TransactionStatus.Paid,
    isRecurring:     true,
    recurrenceType:  RecurrenceType.Monthly,
    attachmentPath:  'attachments/user/file.pdf',
    tags:            [],
    categoryId:      'cat-2',
    categoryName:    'Trabalho',
    categoryIcon:    '💼',
    categoryColor:   '#22c55e',
    subcategoryId:   null,
    subcategoryName: null,
    createdAt:       '2026-03-05T00:00:00Z',
    updatedAt:       null,
  },
]

const renderTable = (onEdit = vi.fn(), onDelete = vi.fn()) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <TransactionTable
        transactions={mockTransactions}
        onEdit={onEdit}
        onDelete={onDelete}
      />
    </QueryClientProvider>
  )
}

describe('TransactionTable', () => {
  it('deve renderizar as transações', () => {
    renderTable()
    expect(screen.getByText('Mercado')).toBeInTheDocument()
    expect(screen.getByText('Freelance')).toBeInTheDocument()
  })

  it('deve exibir estado vazio quando não há transações', () => {
    const qc = new QueryClient()
    render(
      <QueryClientProvider client={qc}>
        <TransactionTable transactions={[]} onEdit={vi.fn()} onDelete={vi.fn()} />
      </QueryClientProvider>
    )
    expect(screen.getByText('Nenhuma transação encontrada.')).toBeInTheDocument()
  })

  it('deve exibir badge de recorrência para transações recorrentes', () => {
    renderTable()
    expect(screen.getByText('Mensal')).toBeInTheDocument()
  })

  it('deve exibir ícone de anexo quando attachmentPath está preenchido', () => {
  renderTable()
  // Verifica pela classe CSS do ícone Lucide
    const paperclips = document.querySelectorAll('.lucide-paperclip')
    expect(paperclips.length).toBeGreaterThan(0)
  })

  it('deve exibir as tags da transação', () => {
    renderTable()
    expect(screen.getByText('alimentação')).toBeInTheDocument()
  })

  it('deve chamar onEdit ao clicar em editar', async () => {
    const onEdit = vi.fn()
    renderTable(onEdit)
    const user = userEvent.setup()

    const editButtons = screen.getAllByTitle('Editar')
    await user.click(editButtons[0])

    expect(onEdit).toHaveBeenCalledWith(mockTransactions[0])
  })

  it('deve chamar onDelete ao clicar em remover', async () => {
    const onDelete = vi.fn()
    renderTable(vi.fn(), onDelete)
    const user = userEvent.setup()

    const deleteButtons = screen.getAllByTitle('Remover')
    await user.click(deleteButtons[0])

    expect(onDelete).toHaveBeenCalledWith(mockTransactions[0])
  })

  it('deve exibir valor negativo para despesas', () => {
    renderTable()
    expect(screen.getByText('-R$ 150,00')).toBeInTheDocument()
  })

  it('deve exibir valor positivo para receitas', () => {
    renderTable()
    expect(screen.getByText('+R$ 3.000,00')).toBeInTheDocument()
  })
})