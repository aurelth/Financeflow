import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import TransactionForm from '@/features/transactions/components/TransactionForm'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType, type Transaction } from '@/features/transactions/types/transaction.types'

const mockMutate = vi.fn()

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useCreateTransaction: () => ({ mutate: mockMutate, isPending: false }),
  useUpdateTransaction: () => ({ mutate: mockMutate, isPending: false }),
  useUploadAttachment:  () => ({ mutate: mockMutate, isPending: false }),
  useRemoveAttachment:  () => ({ mutate: mockMutate, isPending: false }),
  getAttachmentUrl:     (id: string) => `http://localhost/api/transactions/${id}/attachment`,
}))

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({
    data: [
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
    ],
  }),
}))

const mockTransaction: Transaction = {
  id:              'tx-1',
  amount:          1500,
  type:            TransactionType.Expense,
  date:            '2026-03-01T00:00:00Z',
  description:     'Aluguel',
  status:          TransactionStatus.Paid,
  isRecurring:     false,
  recurrenceType:  RecurrenceType.None,
  attachmentPath:  null,
  attachmentName:  null,
  tags:            [],
  categoryId:      'cat-1',
  categoryName:    'Moradia',
  categoryIcon:    'house',
  categoryColor:   '#6366f1',
  subcategoryId:   null,
  subcategoryName: null,
  createdAt:       '2026-03-01T00:00:00Z',
  updatedAt:       null,
}

const mockTransactionWithAttachment: Transaction = {
  ...mockTransaction,
  attachmentPath: 'attachments/user/comprovante.pdf',
}

const renderForm = (transaction?: Transaction, onClose = vi.fn()) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <TransactionForm transaction={transaction} onClose={onClose} />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('TransactionForm', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título correto no modo criação', () => {
    renderForm()
    expect(screen.getByRole('heading', { name: 'Nova transação' })).toBeInTheDocument()
  })

  it('deve renderizar o título correto no modo edição', () => {
    renderForm(mockTransaction)
    expect(screen.getByRole('heading', { name: 'Editar transação' })).toBeInTheDocument()
  })

  it('deve preencher os campos com os valores da transação no modo edição', () => {
    renderForm(mockTransaction)
    expect(screen.getByDisplayValue('Aluguel')).toBeInTheDocument()
    expect(screen.getByDisplayValue('1500')).toBeInTheDocument()
  })

  it('deve chamar onClose ao clicar em cancelar', async () => {
    const onClose = vi.fn()
    renderForm(undefined, onClose)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /cancelar/i }))

    expect(onClose).toHaveBeenCalledOnce()
  })

  it('deve exibir botão de adicionar comprovante quando não há anexo', () => {
    renderForm(mockTransaction)
    expect(screen.getByText('Adicionar comprovante')).toBeInTheDocument()
  })

  it('deve exibir nome do ficheiro e botões de substituir e remover quando há anexo', () => {
    renderForm(mockTransactionWithAttachment)
    expect(screen.getByText('comprovante.pdf')).toBeInTheDocument()
    expect(screen.getByText('Substituir')).toBeInTheDocument()
    expect(screen.getByText('Remover')).toBeInTheDocument()
  })

  it('deve chamar removeAttachment ao clicar em remover comprovante', async () => {
    renderForm(mockTransactionWithAttachment)
    const user = userEvent.setup()

    await user.click(screen.getByText('Remover'))

    expect(mockMutate).toHaveBeenCalledOnce()
  })

  it('deve mostrar campo de recorrência ao marcar transação recorrente', async () => {
    renderForm()
    const user = userEvent.setup()

    await user.click(screen.getByRole('checkbox'))

    await waitFor(() => {
      expect(screen.getByText('Mensal')).toBeInTheDocument()
    })
  })

  it('deve adicionar tag ao pressionar Enter', async () => {
    renderForm()
    const user = userEvent.setup()

    const tagInput = screen.getByPlaceholderText('Adicionar tag...')
    await user.type(tagInput, 'alimentação{Enter}')

    await waitFor(() => {
      expect(screen.getByText('alimentação')).toBeInTheDocument()
    })
  })

  it('deve remover tag ao clicar no botão de remover tag', async () => {
    renderForm({ ...mockTransaction, tags: ['alimentação'] })
    const user = userEvent.setup()

    expect(screen.getByText('alimentação')).toBeInTheDocument()

    // Botão X da tag
    const removeTagButtons = screen.getAllByRole('button').filter(btn =>
      btn.closest('span')?.textContent?.includes('alimentação')
    )
    await user.click(removeTagButtons[0])

    await waitFor(() => {
      expect(screen.queryByText('alimentação')).not.toBeInTheDocument()
    })
  })

  it('deve esconder o comprovante após clicar em remover', async () => {
  const mockRemoveMutate = vi.fn((_, options) => options?.onSuccess?.())
  vi.mocked(mockMutate).mockImplementation(mockRemoveMutate)

  renderForm(mockTransactionWithAttachment)
  const user = userEvent.setup()

  // Comprovante visível inicialmente
  expect(screen.getByText('comprovante.pdf')).toBeInTheDocument()
  expect(screen.getByText('Remover')).toBeInTheDocument()

  await user.click(screen.getByText('Remover'))

  // Comprovante deve desaparecer
  await waitFor(() => {
    expect(screen.queryByText('comprovante.pdf')).not.toBeInTheDocument()
    expect(screen.queryByText('Remover')).not.toBeInTheDocument()
    expect(screen.getByText('Adicionar comprovante')).toBeInTheDocument()
  })
 })

it('deve mostrar botão de adicionar comprovante após remover', async () => {
  const mockRemoveMutate = vi.fn((_, options) => options?.onSuccess?.())
  vi.mocked(mockMutate).mockImplementation(mockRemoveMutate)

  renderForm(mockTransactionWithAttachment)
  const user = userEvent.setup()

  await user.click(screen.getByText('Remover'))

  await waitFor(() => {
    expect(screen.getByText('Adicionar comprovante')).toBeInTheDocument()
    expect(screen.queryByText('Substituir')).not.toBeInTheDocument()
  })
 })
})