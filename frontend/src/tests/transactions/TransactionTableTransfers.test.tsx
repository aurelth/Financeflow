import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import TransactionTable from '@/features/transactions/components/TransactionTable'
import { TransactionType } from '@/features/categories/types/category.types'
import { TransactionStatus, RecurrenceType, type Transaction } from '@/features/transactions/types/transaction.types'

const base: Transaction = {
  id: 'tx-1',
  amount: 1000,
  type: TransactionType.Transfer,
  date: '2026-02-12T00:00:00Z',
  description: 'PIX ENVIADO',
  status: TransactionStatus.Paid,
  isRecurring: false,
  recurrenceType: RecurrenceType.None,
  recurrenceGroupId: null,
  attachmentPath: null,
  attachmentName: null,
  tags: [],
  accountId: 'acc-a',
  accountName: 'Conta A',
  categoryId: 'cat-transfer',
  categoryName: 'Transferência',
  categoryIcon: '↔',
  categoryColor: '#818cf8',
  subcategoryId: null,
  subcategoryName: null,
  createdAt: '2026-02-12T00:00:00Z',
  updatedAt: null,
}

const renderTable = (transactions: Transaction[], onUnlink?: (tx: Transaction) => void) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <TransactionTable transactions={transactions} onEdit={vi.fn()} onDelete={vi.fn()} onUnlink={onUnlink} />
    </QueryClientProvider>,
  )

describe('TransactionTable — transferências entre contas', () => {
  it('deve mostrar "para <conta>" na saída pareada', () => {
    renderTable([{
      ...base,
      transferDirection: 'Out',
      transferLinkId: 'link-1',
      counterpartAccountName: 'Conta B',
    }])

    expect(screen.getByText('para Conta B')).toBeInTheDocument()
  })

  it('deve mostrar "de <conta>" na entrada pareada', () => {
    renderTable([{
      ...base,
      transferDirection: 'In',
      transferLinkId: 'link-1',
      counterpartAccountName: 'Conta A',
    }])

    expect(screen.getByText('de Conta A')).toBeInTheDocument()
  })

  it('deve indicar "sem par" na transferência ainda não pareada', () => {
    renderTable([{ ...base, transferDirection: 'Out', transferLinkId: null }])

    expect(screen.getByText('sem par')).toBeInTheDocument()
  })

  it('não deve indicar par em transferência antiga, sem sentido definido', () => {
    renderTable([{ ...base, transferDirection: null }])

    expect(screen.queryByText('sem par')).not.toBeInTheDocument()
  })

  it('deve oferecer "Desfazer vínculo" só nas transferências pareadas', async () => {
    const onUnlink = vi.fn()
    const linked = { ...base, id: 'tx-linked', transferDirection: 'Out' as const, transferLinkId: 'link-1', counterpartAccountName: 'Conta B' }
    const unlinked = { ...base, id: 'tx-open', description: 'TED SOLTA', transferDirection: 'Out' as const, transferLinkId: null }

    renderTable([linked, unlinked], onUnlink)

    const buttons = screen.getAllByRole('button', { name: /Desfazer vínculo/ })
    expect(buttons).toHaveLength(1)

    await userEvent.setup().click(buttons[0])
    expect(onUnlink).toHaveBeenCalledWith(linked)
  })

  it('não deve mostrar o botão quando a página não trata o desfazer', () => {
    renderTable([{ ...base, transferDirection: 'Out', transferLinkId: 'link-1' }])

    expect(screen.queryByRole('button', { name: /Desfazer vínculo/ })).not.toBeInTheDocument()
  })
})
