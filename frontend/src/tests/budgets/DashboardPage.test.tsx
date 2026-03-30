import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import DashboardPage from '@/features/budgets/pages/DashboardPage'

const mockSummary = {
  totalIncome:   5000,
  totalExpenses: 3200,
  balance:       1800,
  month:         3,
  year:          2026,
}

const mockBudgetSummaries = [
  {
    id:            'budget-1',
    categoryId:    'cat-1',
    categoryName:  'Alimentação',
    categoryIcon:  '🍔',
    categoryColor: '#f97316',
    month:         3,
    year:          2026,
    limitAmount:   500,
    spentAmount:   450,
    percentage:    90,
  },
  {
    id:            'budget-2',
    categoryId:    'cat-2',
    categoryName:  'Transporte',
    categoryIcon:  '🚗',
    categoryColor: '#6366f1',
    month:         3,
    year:          2026,
    limitAmount:   300,
    spentAmount:   100,
    percentage:    33.3,
  },
  {
    id:            'budget-3',
    categoryId:    'cat-3',
    categoryName:  'Lazer',
    categoryIcon:  '🎮',
    categoryColor: '#8b5cf6',
    month:         3,
    year:          2026,
    limitAmount:   200,
    spentAmount:   180,
    percentage:    90,
  },
]

vi.mock('@/features/budgets/api/useBudgets', () => ({
  useDashboardSummary: () => ({ data: mockSummary,         isLoading: false }),
  useBudgetSummary:    () => ({ data: mockBudgetSummaries, isLoading: false }),
  useUpdateBudget:     () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteBudget:     () => ({ mutate: vi.fn(), isPending: false }),
  useCreateBudget:     () => ({ mutate: vi.fn(), isPending: false }),
}))

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({ data: [], isLoading: false }),
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('DashboardPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Dashboard')
  })

  it('deve exibir o total de receitas', () => {
    renderPage()
    expect(screen.getByText('R$ 5.000,00')).toBeInTheDocument()
  })

  it('deve exibir o total de despesas', () => {
    renderPage()
    expect(screen.getByText('R$ 3.200,00')).toBeInTheDocument()
  })

  it('deve exibir o saldo', () => {
    renderPage()
    expect(screen.getByText('R$ 1.800,00')).toBeInTheDocument()
  })

  it('deve exibir o seletor de mês/ano', () => {
    renderPage()
    const now        = new Date()
    const monthLabel = now.toLocaleString('pt-BR', { month: 'long', year: 'numeric' })
    expect(screen.getByText(new RegExp(monthLabel, 'i'))).toBeInTheDocument()
  })

  it('deve exibir apenas os top 3 orçamentos por percentagem', () => {
    renderPage()
    expect(screen.getByText('Alimentação')).toBeInTheDocument()
    expect(screen.getByText('Lazer')).toBeInTheDocument()
    expect(screen.getByText('Transporte')).toBeInTheDocument()
  })

  it('deve exibir o título de orçamentos em destaque', () => {
    renderPage()
    expect(screen.getByText('Orçamentos em destaque')).toBeInTheDocument()
  })

  it('deve navegar para o mês anterior ao clicar em chevron esquerdo', async () => {
    renderPage()
    const user = userEvent.setup()

    const now           = new Date()
    const prevMonth     = new Date(now.getFullYear(), now.getMonth() - 1)
    const prevMonthLabel = prevMonth.toLocaleString('pt-BR', { month: 'long', year: 'numeric' })

    const chevronLeft = document.querySelector('.lucide-chevron-left')?.closest('button')
    await user.click(chevronLeft!)

    await waitFor(() => {
      expect(screen.getByText(new RegExp(prevMonthLabel, 'i'))).toBeInTheDocument()
    })
  })
})