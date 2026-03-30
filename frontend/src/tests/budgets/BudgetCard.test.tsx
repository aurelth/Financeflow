import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import BudgetCard from '@/features/budgets/components/BudgetCard'
import type { BudgetSummary } from '@/features/budgets/types/budget.types'

const mockSummary50: BudgetSummary = {
  id:            'budget-1',
  categoryId:    'cat-1',
  categoryName:  'Alimentação',
  categoryIcon:  '🍔',
  categoryColor: '#f97316',
  month:         3,
  year:          2026,
  limitAmount:   500,
  spentAmount:   250,
  percentage:    50,
}

const mockSummary80: BudgetSummary = {
  ...mockSummary50,
  id:          'budget-2',
  spentAmount: 420,
  percentage:  84,
}

const mockSummary100: BudgetSummary = {
  ...mockSummary50,
  id:          'budget-3',
  spentAmount: 530,
  percentage:  106,
}

const renderCard = (
  summary: BudgetSummary,
  onEdit   = vi.fn(),
  onDelete = vi.fn()
) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <BudgetCard summary={summary} onEdit={onEdit} onDelete={onDelete} />
    </QueryClientProvider>
  )
}

describe('BudgetCard', () => {
  it('deve renderizar o nome da categoria', () => {
    renderCard(mockSummary50)
    expect(screen.getByText('Alimentação')).toBeInTheDocument()
  })

  it('deve exibir a percentagem correta', () => {
    renderCard(mockSummary50)
    expect(screen.getByText('50.0% utilizado')).toBeInTheDocument()
  })

  it('deve exibir os valores de gasto, limite e restante', () => {
    renderCard(mockSummary50)
    expect(screen.getAllByText('R$ 250,00').length).toBe(2) // gasto e restante
    expect(screen.getByText('R$ 500,00')).toBeInTheDocument()
  })

  it('deve exibir alerta de atenção quando percentagem >= 80%', () => {
    renderCard(mockSummary80)
    expect(screen.getByText(/80% do limite atingido/i)).toBeInTheDocument()
  })

  it('deve exibir alerta crítico quando percentagem >= 100%', () => {
    renderCard(mockSummary100)
    expect(screen.getByText(/Limite atingido/i)).toBeInTheDocument()
  })

  it('não deve exibir alertas quando percentagem < 80%', () => {
    renderCard(mockSummary50)
    expect(screen.queryByText(/limite atingido/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/80% do limite/i)).not.toBeInTheDocument()
  })

  it('deve chamar onEdit ao clicar em editar', async () => {
    const onEdit = vi.fn()
    renderCard(mockSummary50, onEdit)
    const user = userEvent.setup()

    const editButton = document.querySelector('.lucide-pencil')?.closest('button')
    await user.click(editButton!)

    expect(onEdit).toHaveBeenCalledOnce()
  })

  it('deve chamar onDelete ao clicar em remover', async () => {
    const onDelete = vi.fn()
    renderCard(mockSummary50, vi.fn(), onDelete)
    const user = userEvent.setup()

    const deleteButton = document.querySelector('.lucide-trash-2')?.closest('button')
    await user.click(deleteButton!)

    expect(onDelete).toHaveBeenCalledOnce()
  })
})