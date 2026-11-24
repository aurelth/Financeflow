import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import InstallmentPlanDialog from '@/features/transactions/components/InstallmentPlanDialog'
import { TransactionStatus } from '@/features/transactions/types/transaction.types'
import type { InstallmentPlan } from '@/features/transactions/types/installment.types'

const mockCancel = vi.fn()
const mockToastSuccess = vi.fn()
let planState: { data?: InstallmentPlan; isLoading: boolean; isError: boolean }
let cancelPending = false

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => (opts ? `${key} ${JSON.stringify(opts)}` : key),
    i18n: { changeLanguage: vi.fn(), language: 'pt-BR' },
  }),
  Trans: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  initReactI18next: { type: '3rdParty', init: vi.fn() },
}))

vi.mock('sonner', () => ({ toast: { success: (...a: unknown[]) => mockToastSuccess(...a), error: vi.fn() } }))

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useInstallmentPlan:    () => planState,
  useCancelInstallments: () => ({ mutate: mockCancel, isPending: cancelPending }),
}))

const plan = (statuses: Array<TransactionStatus | string>): InstallmentPlan => ({
  groupId: 'grp-1', description: 'Notebook', type: 'Expense', count: statuses.length,
  totalAmount: 300, paidAmount: 100, remainingAmount: 200,
  installments: statuses.map((status, i) => ({
    id: `p${i + 1}`, number: i + 1, date: `2026-${String(11 + i).padStart(2, '0')}-24T00:00:00`, amount: 100, status,
  })),
})

const renderDialog = (onClose = vi.fn()) => {
  render(<InstallmentPlanDialog groupId="grp-1" onClose={onClose} />)
  return { onClose }
}

describe('InstallmentPlanDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    cancelPending = false
    planState = { data: plan([TransactionStatus.Paid, 'Scheduled', 'Scheduled']), isLoading: false, isError: false }
  })

  it('mostra o resumo e uma linha por parcela com o status', () => {
    renderDialog()

    const summary = screen.getByTestId('installment-summary')
    expect(summary).toHaveTextContent(/R\$\s300,00/)
    expect(summary).toHaveTextContent(/R\$\s100,00/)
    expect(summary).toHaveTextContent(/R\$\s200,00/)

    const rows = screen.getAllByTestId('installment-row')
    expect(rows).toHaveLength(3)
    expect(within(rows[0]).getByText('status.paid')).toBeInTheDocument()
    expect(within(rows[1]).getByText('status.scheduled')).toBeInTheDocument()
    expect(rows[2]).toHaveTextContent('3/3')
  })

  it('mostra carregando e erro', () => {
    planState = { isLoading: true, isError: false }
    const { unmount } = render(<InstallmentPlanDialog groupId="grp-1" onClose={vi.fn()} />)
    expect(screen.queryByTestId('installment-row')).not.toBeInTheDocument()
    unmount()

    planState = { isLoading: false, isError: true }
    render(<InstallmentPlanDialog groupId="grp-1" onClose={vi.fn()} />)
    expect(screen.getByRole('alert')).toHaveTextContent('installments.loadError')
  })

  it('cancelar a compra pede confirmação antes de chamar a API', async () => {
    renderDialog()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'installments.cancelPurchase' }))

    expect(screen.getByRole('alertdialog')).toHaveTextContent('installments.cancelConfirm')
    expect(mockCancel).not.toHaveBeenCalled()
  })

  it('"manter" desfaz a confirmação sem cancelar', async () => {
    renderDialog()
    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'installments.cancelPurchase' }))

    await user.click(screen.getByRole('button', { name: 'installments.keep' }))

    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
    expect(mockCancel).not.toHaveBeenCalled()
  })

  it('confirmando, cancela o grupo, avisa quantas ficaram e fecha', async () => {
    mockCancel.mockImplementation((_id: string, opts: { onSuccess: (r: unknown) => void }) =>
      opts.onSuccess({ cancelledCount: 2, keptPaidCount: 1 }))
    const { onClose } = renderDialog()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'installments.cancelPurchase' }))
    await user.click(screen.getByRole('button', { name: 'installments.confirmCancel' }))

    expect(mockCancel).toHaveBeenCalledWith('grp-1', expect.any(Object))
    expect(mockToastSuccess).toHaveBeenCalledWith(expect.stringContaining('"cancelled":2'))
    expect(mockToastSuccess).toHaveBeenCalledWith(expect.stringContaining('"kept":1'))
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('com todas as parcelas pagas não oferece cancelar', () => {
    planState = { data: plan([TransactionStatus.Paid, TransactionStatus.Paid]), isLoading: false, isError: false }
    renderDialog()

    expect(screen.queryByRole('button', { name: 'installments.cancelPurchase' })).not.toBeInTheDocument()
    expect(screen.getByText('installments.nothingToCancel')).toBeInTheDocument()
  })

  it('fecha pelo botão fechar', async () => {
    const { onClose } = renderDialog()

    await userEvent.setup().click(screen.getAllByRole('button', { name: 'installments.close' })[0])

    expect(onClose).toHaveBeenCalledOnce()
  })
})
