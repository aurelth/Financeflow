import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ImportsPreviewPage from '@/features/imports/pages/ImportsPreviewPage'
import type { BankImportPreviewDto } from '@/features/imports/types/imports.types'

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return { ...actual, useNavigate: () => vi.fn() }
})

vi.mock('sonner', () => ({
  toast: { error: vi.fn(), success: vi.fn(), warning: vi.fn() },
}))

const mockConfirmMutate = vi.fn()

vi.mock('@/features/imports/api/useImports', () => ({
  useImportPreview: () => ({ data: mockPreview, isLoading: false }),
  useConfirmImport: () => ({ mutate: mockConfirmMutate, isPending: false }),
  useChangeImportAccount: () => ({ mutate: vi.fn(), isPending: false }),
}))

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({
    data: [
      { id: 'cat-1', name: 'Mercado', icon: 'cart', color: '#f59e0b', type: 2, isDefault: true, isActive: true, isOwner: false, isGoalCategory: false, subcategories: [] },
      { id: 'cat-2', name: 'Outras receitas', icon: 'plus', color: '#22c55e', type: 1, isDefault: true, isActive: true, isOwner: false, isGoalCategory: false, subcategories: [] },
    ],
  }),
  useGoalCategories: () => ({ data: [] }),
}))

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts: () => ({
    data: [{ id: 'acc-1', name: 'Conta A', isArchived: false }],
  }),
}))

const basePreview: BankImportPreviewDto = {
  importId: 'imp-1',
  fileName: 'extrato-conta-a.ofx',
  status: 'Completed',
  totalRecords: 3,
  duplicates: 0,
  autoTransfers: 1,
  suggestedTransfers: 1,
  accountId: 'acc-1',
  accountName: 'Conta A',
  transactions: [
    {
      id: 'bt-auto',
      externalId: 'A1',
      date: '2026-02-12T00:00:00',
      amount: 1000,
      description: 'PIX ENVIADO CONTA B',
      type: 'Expense',
      suggestedCategoryId: null,
      isDuplicate: false,
      isSelected: true,
      transferMatch: {
        transactionId: 'tx-b-in',
        accountId: 'acc-b',
        accountName: 'Conta B',
        date: '2026-02-12T00:00:00',
        amount: 1000,
        description: 'PIX RECEBIDO CONTA A',
        score: 80,
        status: 'Auto',
        willConvert: true,
      },
    },
    {
      id: 'bt-suggested',
      externalId: 'A2',
      date: '2026-02-13T00:00:00',
      amount: 200,
      description: 'PAGAMENTO JOAO',
      type: 'Income',
      suggestedCategoryId: null,
      isDuplicate: false,
      isSelected: true,
      transferMatch: {
        transactionId: 'tx-b-out',
        accountId: 'acc-b',
        accountName: 'Conta B',
        date: '2026-02-12T00:00:00',
        amount: 200,
        description: 'Compra qualquer',
        score: 60,
        status: 'Suggested',
        willConvert: true,
      },
    },
    {
      id: 'bt-plain',
      externalId: 'A3',
      date: '2026-02-14T00:00:00',
      amount: 50,
      description: 'MERCADO CENTRAL',
      type: 'Expense',
      suggestedCategoryId: 'cat-1',
      isDuplicate: false,
      isSelected: true,
      transferMatch: null,
    },
  ],
}

let mockPreview: BankImportPreviewDto = basePreview

const renderPage = () =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter initialEntries={['/imports/imp-1/preview']}>
        <Routes>
          <Route path="/imports/:id/preview" element={<ImportsPreviewPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )

const confirmButton = () => screen.getByRole('button', { name: /confirmar/i })

describe('Preview da importação — transferências entre contas', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockPreview = basePreview
  })

  it('deve agrupar o par automático em "Transferências entre contas"', () => {
    renderPage()

    expect(screen.getByText(/Transferências entre contas \(1\)/)).toBeInTheDocument()

    const pair = screen.getByTestId('transfer-pair-bt-auto')
    expect(pair).toHaveTextContent('Par: Conta B')
    expect(pair).toHaveTextContent('PIX RECEBIDO CONTA A')
    expect(pair).toHaveTextContent('pareada automaticamente')
    expect(pair).toHaveTextContent('será convertido em transferência')
  })

  it('deve agrupar a sugestão em "Possíveis transferências", sem vincular', () => {
    renderPage()

    expect(screen.getByText(/Possíveis transferências \(1\)/)).toBeInTheDocument()

    const pair = screen.getByTestId('transfer-pair-bt-suggested')
    expect(pair).toHaveTextContent('Possível transferência com Conta B')
    expect(within(pair).getByRole('button', { name: /Vincular/ })).toBeInTheDocument()
  })

  it('deve manter as demais linhas em "Novas transações"', () => {
    renderPage()

    expect(screen.getByText(/Novas transações \(1\)/)).toBeInTheDocument()
    expect(screen.getByText('MERCADO CENTRAL')).toBeInTheDocument()
  })

  it('não deve pedir categoria para a transferência vinculada', () => {
    renderPage()

    // A linha vinculada não tem seletor de categoria e não bloqueia a confirmação
    expect(screen.queryByRole('combobox', { name: 'Categoria de PIX ENVIADO CONTA B' })).not.toBeInTheDocument()
  })

  it('deve contar as transferências pareadas no resumo', () => {
    renderPage()

    expect(screen.getByText(/1 transferência pareada/)).toBeInTheDocument()
  })

  it('deve vincular a sugestão ao clicar em "Vincular"', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Vincular: PAGAMENTO JOAO' }))

    expect(screen.getByText(/Transferências entre contas \(2\)/)).toBeInTheDocument()
    expect(screen.getByTestId('transfer-pair-bt-suggested')).toHaveTextContent('Par: Conta B')
    expect(screen.getByText(/2 transferências pareadas/)).toBeInTheDocument()
  })

  it('deve desfazer o vínculo e passar a exigir a categoria da linha', async () => {
    renderPage()
    const user = userEvent.setup()

    // A sugestão não aceita é uma receita comum e precisa de categoria
    await user.selectOptions(
      screen.getByRole('combobox', { name: 'Categoria de PAGAMENTO JOAO' }),
      'cat-2',
    )
    await waitFor(() => expect(confirmButton()).not.toBeDisabled())

    await user.click(screen.getByRole('button', { name: 'Desfazer vínculo: PIX ENVIADO CONTA B' }))

    // A linha volta a ser uma despesa comum sem categoria: a confirmação é bloqueada
    expect(screen.getByRole('combobox', { name: 'Categoria de PIX ENVIADO CONTA B' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Vincular: PIX ENVIADO CONTA B/ })).toBeInTheDocument()
    await waitFor(() => expect(confirmButton()).toBeDisabled())
  })

  it('deve enviar a decisão de vínculo de cada par na confirmação', async () => {
    renderPage()
    const user = userEvent.setup()

    // A sugestão ignorada precisa de categoria para poder ser confirmada
    await user.selectOptions(
      screen.getByRole('combobox', { name: 'Categoria de PAGAMENTO JOAO' }),
      'cat-2',
    )
    await waitFor(() => expect(confirmButton()).not.toBeDisabled())

    await user.click(confirmButton())

    expect(mockConfirmMutate).toHaveBeenCalledTimes(1)
    const [{ transactions }] = mockConfirmMutate.mock.calls[0]

    expect(transactions).toEqual(
      expect.arrayContaining([
        // Par automático vinculado: vira transferência sem categoria
        expect.objectContaining({ id: 'bt-auto', type: 'Transfer', transferAction: 'Link' }),
        // Sugestão não aceita: continua receita e o par é recusado explicitamente
        expect.objectContaining({ id: 'bt-suggested', type: 'Income', categoryId: 'cat-2', transferAction: 'Ignore' }),
        // Linha sem par não leva ação de transferência
        expect.objectContaining({ id: 'bt-plain', type: 'Expense', categoryId: 'cat-1' }),
      ]),
    )
    const plain = transactions.find((t: { id: string }) => t.id === 'bt-plain')
    expect(plain).not.toHaveProperty('transferAction')
  })

  it('deve enviar "Link" para a sugestão aceita pelo usuário', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Vincular: PAGAMENTO JOAO' }))
    await waitFor(() => expect(confirmButton()).not.toBeDisabled())
    await user.click(confirmButton())

    const [{ transactions }] = mockConfirmMutate.mock.calls[0]
    expect(transactions).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ id: 'bt-suggested', type: 'Transfer', transferAction: 'Link' }),
      ]),
    )
  })
})
