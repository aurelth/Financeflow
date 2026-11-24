import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import TransactionsPage from '@/features/transactions/pages/TransactionsPage'
import type { QuickEntryBatch, QuickEntryDraft } from '@/features/quick-entry/types/quickEntry.types'

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useTransactions:      () => ({ data: { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1 }, isLoading: false }),
  useCreateTransaction: () => ({ mutate: vi.fn(), isPending: false }),
  useUpdateTransaction: () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteTransaction: () => ({ mutate: vi.fn(), isPending: false }),
  useUploadAttachment:  () => ({ mutate: vi.fn(), isPending: false }),
  useRemoveAttachment:  () => ({ mutate: vi.fn(), isPending: false }),
}))
vi.mock('@/features/accounts/api/useAccounts', () => ({ useAccounts: () => ({ data: [] }) }))
vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({ data: [], isLoading: false }),
  useGoalCategories: () => ({ data: [], isLoading: false }),
}))

const drafts: QuickEntryDraft[] = [
  { type: 'Expense', amount: 10, date: '2026-11-23T00:00:00', description: 'Primeiro', categoryId: null, categoryName: null },
  { type: 'Expense', amount: 20, date: '2026-11-23T00:00:00', description: 'Segundo',  categoryId: null, categoryName: null },
]

// O diálogo em si já tem testes próprios: aqui ele só entrega um lote, para testar a fila da página
vi.mock('@/features/quick-entry/components/QuickEntryDialog', () => ({
  default: ({ onResult, onClose }: { onResult: (b: QuickEntryBatch) => void; onClose: () => void }) => (
    <div data-testid="quick-entry-dialog">
      <button onClick={() => onResult({ drafts, image: null })}>entregar-lote</button>
      <button onClick={onClose}>fechar-dialogo</button>
    </div>
  ),
}))

const renderPage = () => render(
  <QueryClientProvider client={new QueryClient()}>
    <MemoryRouter><TransactionsPage /></MemoryRouter>
  </QueryClientProvider>
)

describe('TransactionsPage — lançamento por IA', () => {
  beforeEach(() => vi.clearAllMocks())

  it('abre e fecha o diálogo pelo botão "Lançar com IA"', async () => {
    renderPage()
    const user = userEvent.setup()

    expect(screen.queryByTestId('quick-entry-dialog')).not.toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /quickEntry\.button/ }))
    expect(screen.getByTestId('quick-entry-dialog')).toBeInTheDocument()

    await user.click(screen.getByText('fechar-dialogo'))
    expect(screen.queryByTestId('quick-entry-dialog')).not.toBeInTheDocument()
  })

  it('abre um formulário por rascunho, em ordem, e encerra a fila no último', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /quickEntry\.button/ }))
    await user.click(screen.getByText('entregar-lote'))

    expect(screen.queryByTestId('quick-entry-dialog')).not.toBeInTheDocument()
    expect(screen.getByDisplayValue('Primeiro')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /cancelar/i }))
    expect(screen.getByDisplayValue('Segundo')).toBeInTheDocument()
    expect(screen.queryByDisplayValue('Primeiro')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /cancelar/i }))
    expect(screen.queryByTestId('ai-draft-banner')).not.toBeInTheDocument()
  })

  it('"Descartar restantes" encerra a fila de uma vez', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /quickEntry\.button/ }))
    await user.click(screen.getByText('entregar-lote'))
    await user.click(screen.getByRole('button', { name: 'quickEntry.discardRest' }))

    expect(screen.queryByTestId('ai-draft-banner')).not.toBeInTheDocument()
    expect(screen.queryByDisplayValue('Segundo')).not.toBeInTheDocument()
  })
})
