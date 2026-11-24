import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import TransactionForm from '@/features/transactions/components/TransactionForm'
import { TransactionType } from '@/features/categories/types/category.types'
import type { QuickEntryDraft } from '@/features/quick-entry/types/quickEntry.types'

const mockCreate = vi.fn()

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts: () => ({ data: [] }),
}))

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useCreateTransaction: () => ({ mutate: mockCreate, isPending: false }),
  useUpdateTransaction: () => ({ mutate: vi.fn(), isPending: false }),
  useUploadAttachment:  () => ({ mutate: vi.fn(), isPending: false }),
  useRemoveAttachment:  () => ({ mutate: vi.fn(), isPending: false }),
  getAttachmentUrl: (id: string) => `http://localhost/api/transactions/${id}/attachment`,
}))

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({
    data: [
      { id: 'cat-1', name: 'Restaurantes', icon: 'x', color: '#6366f1', type: TransactionType.Expense,
        isDefault: true, isActive: true, isOwner: false, isGoalCategory: false, subcategories: [] },
      { id: 'cat-2', name: 'Salário', icon: 'x', color: '#22c55e', type: TransactionType.Income,
        isDefault: true, isActive: true, isOwner: false, isGoalCategory: false, subcategories: [] },
    ],
  }),
  useGoalCategories: () => ({ data: [] }),
}))

const draft: QuickEntryDraft = {
  type: 'Expense', amount: 45.9, date: '2026-11-23T00:00:00', description: 'Almoço iFood',
  categoryId: 'cat-1', categoryName: 'Restaurantes',
}

function renderDraft(props: Partial<React.ComponentProps<typeof TransactionForm>> = {}) {
  const onClose = vi.fn()
  render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>
        <TransactionForm draft={draft} onClose={onClose} {...props} />
      </MemoryRouter>
    </QueryClientProvider>
  )
  return { onClose }
}

describe('TransactionForm com rascunho da IA', () => {
  beforeEach(() => vi.clearAllMocks())

  it('abre preenchido com os valores sugeridos e avisa que veio da IA', () => {
    renderDraft()

    expect(screen.getByTestId('ai-draft-banner')).toHaveTextContent('quickEntry.draftBanner')
    expect(screen.getByDisplayValue('Almoço iFood')).toBeInTheDocument()
    expect(screen.getByDisplayValue('45.9')).toBeInTheDocument()
    expect(screen.getByDisplayValue('2026-11-23')).toBeInTheDocument()
  })

  it('mantém a categoria sugerida (não é zerada pelo efeito de troca de tipo)', async () => {
    renderDraft()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Criar' }))

    expect(mockCreate).toHaveBeenCalledOnce()
    expect(mockCreate.mock.calls[0][0].data).toMatchObject({
      amount: 45.9, type: TransactionType.Expense, description: 'Almoço iFood', categoryId: 'cat-1', date: '2026-11-23',
    })
  })

  it('anexa o comprovante da foto ao salvar', async () => {
    const photo = new File(['x'], 'cupom.jpg', { type: 'image/jpeg' })
    renderDraft({ draftAttachment: photo })
    const user = userEvent.setup()

    expect(screen.getByText('cupom.jpg')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Criar' }))

    expect(mockCreate.mock.calls[0][0].attachment).toBe(photo)
  })

  it('converte receita sugerida pela IA para o tipo Receita', () => {
    renderDraft({ draft: { ...draft, type: 'Income', categoryId: 'cat-2', categoryName: 'Salário' } })

    expect(screen.getByRole('button', { name: /receita/i })).toHaveStyle({ color: 'var(--ff-income)' })
  })

  it('sem categoria sugerida, não deixa criar até o usuário escolher', async () => {
    renderDraft({ draft: { ...draft, categoryId: null, categoryName: null } })
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Criar' }))

    expect(mockCreate).not.toHaveBeenCalled()
  })

  it('mostra o progresso e permite descartar os restantes quando há vários rascunhos', async () => {
    const onDiscardDrafts = vi.fn()
    renderDraft({ draftProgress: { current: 2, total: 3 }, onDiscardDrafts })
    const user = userEvent.setup()

    expect(screen.getByTestId('ai-draft-progress')).toHaveTextContent('quickEntry.draftProgress')
    await user.click(screen.getByRole('button', { name: 'quickEntry.discardRest' }))

    expect(onDiscardDrafts).toHaveBeenCalledOnce()
  })

  it('com um único rascunho não mostra progresso nem "descartar restantes"', () => {
    renderDraft({ draftProgress: { current: 1, total: 1 }, onDiscardDrafts: vi.fn() })

    expect(screen.queryByTestId('ai-draft-progress')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'quickEntry.discardRest' })).not.toBeInTheDocument()
  })

  it('não mostra o aviso fora do fluxo de IA', () => {
    renderDraft({ draft: undefined })
    expect(screen.queryByTestId('ai-draft-banner')).not.toBeInTheDocument()
  })
})
