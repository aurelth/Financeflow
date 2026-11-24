import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import TransactionForm from '@/features/transactions/components/TransactionForm'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType, type Transaction } from '@/features/transactions/types/transaction.types'
import type { QuickEntryDraft } from '@/features/quick-entry/types/quickEntry.types'
import { makeAccount } from '../accounts/fixtures'

const mockCreate = vi.fn()
const mockUpdate = vi.fn()

// t que também devolve as opções de interpolação, para conferir os valores da prévia
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key} ${JSON.stringify(opts)}` : key),
    i18n: { changeLanguage: vi.fn(), language: 'pt-BR' },
  }),
  Trans: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  initReactI18next: { type: '3rdParty', init: vi.fn() },
}))

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts: () => ({ data: [makeAccount()] }),
}))

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useCreateTransaction: () => ({ mutate: mockCreate, isPending: false }),
  useUpdateTransaction: () => ({ mutate: mockUpdate, isPending: false }),
  useUploadAttachment:  () => ({ mutate: vi.fn(), isPending: false }),
  useRemoveAttachment:  () => ({ mutate: vi.fn(), isPending: false }),
  getAttachmentUrl: (id: string) => `http://localhost/api/transactions/${id}/attachment`,
}))

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({
    data: [
      { id: 'cat-1', name: 'Eletrônicos', icon: 'x', color: '#6366f1', type: TransactionType.Expense,
        isDefault: true, isActive: true, isOwner: false, isGoalCategory: false, subcategories: [] },
    ],
  }),
  useGoalCategories: () => ({ data: [] }),
}))

const draft: QuickEntryDraft = {
  type: 'Expense', amount: 1200, date: '2026-11-24T00:00:00', description: 'Notebook',
  categoryId: 'cat-1', categoryName: 'Eletrônicos',
}

const installmentTx: Transaction = {
  id: 'tx-p2', amount: 100, type: TransactionType.Expense, date: '2026-12-24T00:00:00',
  description: 'Notebook', status: TransactionStatus.Scheduled, isRecurring: false,
  recurrenceType: RecurrenceType.None, recurrenceGroupId: null, attachmentPath: null, attachmentName: null,
  tags: [], accountId: 'acc-1', accountName: 'Conta principal', categoryId: 'cat-1', categoryName: 'Eletrônicos',
  categoryIcon: 'x', categoryColor: '#6366f1', subcategoryId: null, subcategoryName: null,
  createdAt: '2026-11-24T00:00:00Z', updatedAt: null,
  installmentGroupId: 'grp-1', installmentNumber: 2, installmentCount: 12,
}

function renderForm(props: Partial<React.ComponentProps<typeof TransactionForm>> = {}) {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <MemoryRouter>
        <TransactionForm draft={draft} onClose={vi.fn()} {...props} />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const toggle = () => screen.getByRole('checkbox', { name: /installments\.toggle/ })
const countInput = () => screen.getByLabelText(/installments\.count/) as HTMLInputElement
const setCount = (value: string) => fireEvent.change(countInput(), { target: { value } })

describe('TransactionForm — compra parcelada (criação)', () => {
  beforeEach(() => vi.clearAllMocks())

  it('não mostra o bloco de parcelas até marcar a opção', () => {
    renderForm()

    expect(toggle()).not.toBeChecked()
    expect(screen.queryByLabelText(/installments\.count/)).not.toBeInTheDocument()
    expect(screen.queryByTestId('installment-preview')).not.toBeInTheDocument()
  })

  it('exibe a prévia 12× de R$ 100,00 com o valor total dividido', async () => {
    renderForm()
    await userEvent.setup().click(toggle())

    const preview = screen.getByTestId('installment-preview')
    expect(preview).toHaveTextContent('installments.previewEqual')
    expect(preview).toHaveTextContent('"count":12')
    expect(preview).toHaveTextContent(/R\$\s100,00/)
  })

  it('mostra a prévia desigual quando sobram centavos (R$ 100 em 7 parcelas)', async () => {
    renderForm({ draft: { ...draft, amount: 100 } })
    await userEvent.setup().click(toggle())
    setCount('7')

    const preview = screen.getByTestId('installment-preview')
    expect(preview).toHaveTextContent('installments.previewUneven')
    expect(preview).toHaveTextContent(/R\$\s14,29/)
    expect(preview).toHaveTextContent(/R\$\s14,28/)
  })

  it('rotula o campo de valor como total enquanto parcela', async () => {
    renderForm()
    expect(screen.queryByText('installments.totalAmount')).not.toBeInTheDocument()

    await userEvent.setup().click(toggle())

    expect(screen.getByText('installments.totalAmount')).toBeInTheDocument()
  })

  it('envia installmentCount junto com os dados ao criar', async () => {
    renderForm()
    const user = userEvent.setup()
    await user.click(toggle())
    setCount('10')

    await user.click(screen.getByRole('button', { name: 'Criar' }))

    expect(mockCreate).toHaveBeenCalledOnce()
    expect(mockCreate.mock.calls[0][0].data).toMatchObject({ amount: 1200, installmentCount: 10 })
  })

  it('sem marcar a opção, não envia installmentCount', async () => {
    renderForm()

    await userEvent.setup().click(screen.getByRole('button', { name: 'Criar' }))

    expect(mockCreate.mock.calls[0][0].data).not.toHaveProperty('installmentCount')
  })

  it.each(['1', '61', '0', ''])('bloqueia a criação com %s parcela(s) e avisa', async value => {
    renderForm()
    const user = userEvent.setup()
    await user.click(toggle())
    setCount(value)

    expect(screen.getByText('installments.invalidCount')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Criar' })).toBeDisabled()
    expect(screen.queryByTestId('installment-preview')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Criar' }))
    expect(mockCreate).not.toHaveBeenCalled()
  })

  it('bloqueia quando o total não dá ao menos um centavo por parcela', async () => {
    renderForm({ draft: { ...draft, amount: 0.1 } })
    await userEvent.setup().click(toggle())

    expect(screen.getByText('installments.tooSmall')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Criar' })).toBeDisabled()
  })

  it('esconde a recorrência enquanto parcela e a oferece de volta ao desmarcar', async () => {
    renderForm()
    const user = userEvent.setup()
    const recurring = () => screen.getByRole('checkbox', { name: /transação recorrente/i })

    await user.click(toggle())
    expect(recurring()).toBeDisabled()

    await user.click(toggle())
    expect(recurring()).toBeEnabled()
  })

  it('não oferece parcelamento para recorrente', async () => {
    renderForm()
    const user = userEvent.setup()

    await user.click(screen.getByRole('checkbox', { name: /transação recorrente/i }))

    expect(screen.queryByRole('checkbox', { name: /installments\.toggle/ })).not.toBeInTheDocument()
  })
})

describe('TransactionForm — editar parcela', () => {
  beforeEach(() => vi.clearAllMocks())

  it('mostra o aviso "Parcela N de M" e não oferece novo parcelamento', () => {
    renderForm({ draft: undefined, transaction: installmentTx })

    const banner = screen.getByTestId('installment-edit-banner')
    expect(banner).toHaveTextContent('installments.editBanner')
    expect(banner).toHaveTextContent('"number":2')
    expect(banner).toHaveTextContent('"count":12')
    expect(screen.queryByRole('checkbox', { name: /installments\.toggle/ })).not.toBeInTheDocument()
  })

  it('não permite trocar o tipo da parcela', () => {
    renderForm({ draft: undefined, transaction: installmentTx })

    expect(screen.getByRole('button', { name: /receita/i })).toBeDisabled()
  })

  it('ao alterar o valor, pergunta se aplica só nesta parcela ou nas seguintes', async () => {
    renderForm({ draft: undefined, transaction: installmentTx })
    const user = userEvent.setup()

    const amount = screen.getByDisplayValue('100')
    await user.clear(amount)
    await user.type(amount, '150')
    await user.click(screen.getByRole('button', { name: /atualizar/i }))

    expect(screen.getByText('installments.propagateTitle')).toBeInTheDocument()
    expect(screen.getByText('installments.propagateOnly')).toBeInTheDocument()
    expect(screen.getByText('installments.propagateFuture')).toBeInTheDocument()
    expect(screen.queryByText('Editar transação recorrente')).not.toBeInTheDocument()
    expect(mockUpdate).not.toHaveBeenCalled()
  })

  it.each([
    ['installments.propagateFuture', true],
    ['installments.propagateOnly',   false],
  ])('escolher "%s" envia propagateToFuture=%s', async (label, expected) => {
    renderForm({ draft: undefined, transaction: installmentTx })
    const user = userEvent.setup()
    const amount = screen.getByDisplayValue('100')
    await user.clear(amount)
    await user.type(amount, '150')
    await user.click(screen.getByRole('button', { name: /atualizar/i }))

    await user.click(screen.getByRole('button', { name: new RegExp(label.replace('.', '\\.')) }))

    expect(mockUpdate).toHaveBeenCalledOnce()
    expect(mockUpdate.mock.calls[0][0]).toMatchObject({ amount: 150, propagateToFuture: expected })
  })
})
