import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import {
  useCancelInstallments, useCreateTransaction, useInstallmentPlan,
} from '@/features/transactions/api/useTransactions'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType } from '@/features/transactions/types/transaction.types'

const mockGet    = vi.fn()
const mockPost   = vi.fn()
const mockDelete = vi.fn()

vi.mock('@/lib/axios', () => ({
  default: {
    get:    (...a: unknown[]) => mockGet(...a),
    post:   (...a: unknown[]) => mockPost(...a),
    delete: (...a: unknown[]) => mockDelete(...a),
  },
}))
vi.mock('sonner', () => ({ toast: { success: vi.fn(), error: vi.fn() } }))

function setup() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const invalidate = vi.spyOn(qc, 'invalidateQueries')
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={qc}>{children}</QueryClientProvider>
  )
  return { invalidate, wrapper }
}

const invalidatedKeys = (spy: { mock: { calls: unknown[][] } }) =>
  spy.mock.calls.map(c => (c[0] as { queryKey: string[] }).queryKey[0])

describe('hooks de compra parcelada', () => {
  beforeEach(() => vi.clearAllMocks())

  it('useInstallmentPlan busca o plano do grupo', async () => {
    mockGet.mockResolvedValue({ data: { groupId: 'grp-1', installments: [] } })
    const { wrapper } = setup()

    const { result } = renderHook(() => useInstallmentPlan('grp-1'), { wrapper })

    await waitFor(() => expect(result.current.data).toEqual({ groupId: 'grp-1', installments: [] }))
    expect(mockGet).toHaveBeenCalledWith('/api/transactions/installments/grp-1')
  })

  it('useInstallmentPlan não busca sem grupo', () => {
    const { wrapper } = setup()

    renderHook(() => useInstallmentPlan(null), { wrapper })

    expect(mockGet).not.toHaveBeenCalled()
  })

  it('useCancelInstallments chama DELETE e atualiza listas, saldos e orçamentos', async () => {
    mockDelete.mockResolvedValue({ data: { cancelledCount: 3, keptPaidCount: 1 } })
    const { wrapper, invalidate } = setup()

    const { result } = renderHook(() => useCancelInstallments(), { wrapper })
    let out: unknown
    await act(async () => { out = await result.current.mutateAsync('grp-1') })

    expect(out).toEqual({ cancelledCount: 3, keptPaidCount: 1 })
    expect(mockDelete).toHaveBeenCalledWith('/api/transactions/installments/grp-1')
    expect(invalidatedKeys(invalidate)).toEqual(expect.arrayContaining(['transactions', 'dashboard', 'accounts', 'budgets']))
  })

  it('criar com installmentCount envia o campo no multipart e invalida orçamentos', async () => {
    mockPost.mockResolvedValue({ data: { id: 'tx-1' } })
    const { wrapper, invalidate } = setup()

    const { result } = renderHook(() => useCreateTransaction(), { wrapper })
    await act(() => result.current.mutateAsync({
      data: {
        amount: 1200, type: TransactionType.Expense, date: '2026-11-24', description: 'Notebook',
        status: TransactionStatus.Pending, isRecurring: false, recurrenceType: RecurrenceType.None,
        categoryId: 'cat-1', subcategoryId: null, accountId: 'acc-1', tags: [], installmentCount: 12,
      },
    }))

    const [url, body] = mockPost.mock.calls[0]
    expect(url).toBe('/api/transactions')
    expect(body).toBeInstanceOf(FormData)
    expect((body as FormData).get('installmentCount')).toBe('12')
    expect(invalidatedKeys(invalidate)).toContain('budgets')
  })

  it('criar sem installmentCount não envia o campo', async () => {
    mockPost.mockResolvedValue({ data: { id: 'tx-1' } })
    const { wrapper } = setup()

    const { result } = renderHook(() => useCreateTransaction(), { wrapper })
    await act(() => result.current.mutateAsync({
      data: {
        amount: 10, type: TransactionType.Expense, date: '2026-11-24', description: 'Café',
        status: TransactionStatus.Paid, isRecurring: false, recurrenceType: RecurrenceType.None,
        categoryId: 'cat-1', subcategoryId: null, accountId: 'acc-1', tags: [],
      },
    }))

    expect((mockPost.mock.calls[0][1] as FormData).has('installmentCount')).toBe(false)
  })
})
