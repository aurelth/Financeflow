import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import BudgetsPage from '@/features/budgets/pages/BudgetsPage'

const mockSummaries = [
  {
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
    spentAmount:   280,
    percentage:    93.3,
  },
]

vi.mock('@/features/budgets/api/useBudgets', () => ({
  useBudgetSummary: () => ({ data: mockSummaries, isLoading: false }),
  useCreateBudget:  () => ({ mutate: vi.fn(), isPending: false }),
  useUpdateBudget:  () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteBudget:  () => ({ mutate: vi.fn(), isPending: false }),
}))

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories: () => ({ data: [], isLoading: false }),
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <BudgetsPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('BudgetsPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Orçamentos')
  })

  it('deve exibir todos os orçamentos', () => {
    renderPage()
    expect(screen.getByText('Alimentação')).toBeInTheDocument()
    expect(screen.getByText('Transporte')).toBeInTheDocument()
  })

  it('deve exibir o seletor de mês/ano', () => {
    renderPage()
    const now        = new Date()
    const monthLabel = now.toLocaleString('pt-BR', { month: 'long', year: 'numeric' })
    expect(screen.getByText(new RegExp(monthLabel, 'i'))).toBeInTheDocument()
  })

  it('deve abrir modal ao clicar em novo orçamento', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /novo orçamento/i }))

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Novo orçamento' })).toBeInTheDocument()
    })
  })

  it('deve exibir alerta de atenção para orçamentos acima de 80%', () => {
    renderPage()
    expect(screen.getByText(/80% do limite atingido/i)).toBeInTheDocument()
  })

  it('deve navegar para o mês anterior ao clicar em chevron esquerdo', async () => {
    renderPage()
    const user = userEvent.setup()

    const now            = new Date()
    const prevMonth      = new Date(now.getFullYear(), now.getMonth() - 1)
    const prevMonthLabel = prevMonth.toLocaleString('pt-BR', { month: 'long', year: 'numeric' })

    const chevronLeft = document.querySelector('.lucide-chevron-left')?.closest('button')
    await user.click(chevronLeft!)

    await waitFor(() => {
      expect(screen.getByText(new RegExp(prevMonthLabel, 'i'))).toBeInTheDocument()
    })
  })
})