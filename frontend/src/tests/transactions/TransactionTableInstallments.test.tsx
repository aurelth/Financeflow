import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import TransactionTable from '@/features/transactions/components/TransactionTable'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType, type Transaction } from '@/features/transactions/types/transaction.types'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key} ${JSON.stringify(opts)}` : key),
    i18n: { changeLanguage: vi.fn(), language: 'pt-BR' },
  }),
  Trans: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  initReactI18next: { type: '3rdParty', init: vi.fn() },
}))

const base: Transaction = {
  id: 'tx-1', amount: 100, type: TransactionType.Expense, date: '2026-12-24T00:00:00Z',
  description: 'Notebook', status: TransactionStatus.Scheduled, isRecurring: false,
  recurrenceType: RecurrenceType.None, recurrenceGroupId: null, attachmentPath: null, attachmentName: null,
  tags: [], accountId: 'acc-1', accountName: 'Conta principal', categoryId: 'cat-1', categoryName: 'Eletrônicos',
  categoryIcon: '💻', categoryColor: '#6366f1', subcategoryId: null, subcategoryName: null,
  createdAt: '2026-11-24T00:00:00Z', updatedAt: null,
}

const parcel: Transaction = { ...base, id: 'tx-p3', installmentGroupId: 'grp-1', installmentNumber: 3, installmentCount: 12 }

const renderTable = (transactions: Transaction[], onShowInstallments?: (t: Transaction) => void) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <TransactionTable
        transactions={transactions}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        onShowInstallments={onShowInstallments}
      />
    </QueryClientProvider>,
  )

describe('TransactionTable — parcelas', () => {
  it('mostra o selo N/M na parcela', () => {
    renderTable([parcel], vi.fn())

    expect(screen.getByTestId('installment-badge')).toHaveTextContent('3/12')
  })

  it('não mostra selo em transação comum', () => {
    renderTable([base], vi.fn())

    expect(screen.queryByTestId('installment-badge')).not.toBeInTheDocument()
  })

  it('clicar no selo abre o plano daquela compra', async () => {
    const onShow = vi.fn()
    renderTable([parcel], onShow)

    await userEvent.setup().click(screen.getByTestId('installment-badge'))

    expect(onShow).toHaveBeenCalledWith(parcel)
  })

  it('sem handler, o selo aparece mas fica desabilitado', () => {
    renderTable([parcel])

    expect(screen.getByTestId('installment-badge')).toBeDisabled()
  })
})
