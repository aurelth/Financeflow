import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import DashboardPage from '@/features/dashboard/pages/DashboardPage'

const mockSummary = {
  totalIncome:      5000,
  totalExpenses:    3200,
  balance:          1800,
  projectedBalance: 2300,
  month:            3,
  year:             2026,
}

const mockBalanceEvolution = [
  { date: '2026-03-01', income: 5000, expenses: 0,    balance: 5000 },
  { date: '2026-03-02', income: 0,    expenses: 200,  balance: 4800 },
  { date: '2026-03-03', income: 0,    expenses: 3000, balance: 1800 },
]

const mockExpensesByCategory = [
  { categoryId: 'cat-1', categoryName: 'Alimentação', categoryIcon: '🍔', categoryColor: '#f97316', total: 2000, percentage: 62.5 },
  { categoryId: 'cat-2', categoryName: 'Transporte',  categoryIcon: '🚗', categoryColor: '#6366f1', total: 1200, percentage: 37.5 },
]

const mockWeeklyComparison = [
  { week: 1, label: 'Sem 1 (03/01-07)', income: 5000, expenses: 200  },
  { week: 2, label: 'Sem 2 (03/08-14)', income: 0,    expenses: 1000 },
  { week: 3, label: 'Sem 3 (03/15-21)', income: 0,    expenses: 2000 },
  { week: 4, label: 'Sem 4 (03/22-31)', income: 0,    expenses: 0    },
]

const mockBudgetSummaries = [
  { id: 'budget-1', categoryId: 'cat-1', categoryName: 'Alimentação', categoryIcon: '🍔', categoryColor: '#f97316', month: 3, year: 2026, limitAmount: 500,  spentAmount: 450, percentage: 90   },
  { id: 'budget-2', categoryId: 'cat-2', categoryName: 'Transporte',  categoryIcon: '🚗', categoryColor: '#6366f1', month: 3, year: 2026, limitAmount: 300,  spentAmount: 100, percentage: 33.3 },
  { id: 'budget-3', categoryId: 'cat-3', categoryName: 'Lazer',       categoryIcon: '🎮', categoryColor: '#8b5cf6', month: 3, year: 2026, limitAmount: 200,  spentAmount: 180, percentage: 90   },
]

const mockTransactions = {
  items: [
    { id: 'tx-1', amount: 5000, type: 1, date: '2026-03-01T00:00:00Z', description: 'Salário', status: 1, isRecurring: false, recurrenceType: 0, attachmentPath: null, attachmentName: null, tags: [], categoryId: 'cat-2', categoryName: 'Trabalho', categoryIcon: '💼', categoryColor: '#22c55e', subcategoryId: null, subcategoryName: null, createdAt: '2026-03-01T00:00:00Z', updatedAt: null },
    { id: 'tx-2', amount: 200,  type: 2, date: '2026-03-02T00:00:00Z', description: 'Mercado', status: 1, isRecurring: false, recurrenceType: 0, attachmentPath: null, attachmentName: null, tags: [], categoryId: 'cat-1', categoryName: 'Alimentação', categoryIcon: '🍔', categoryColor: '#f97316', subcategoryId: null, subcategoryName: null, createdAt: '2026-03-02T00:00:00Z', updatedAt: null },
  ],
  totalCount: 2,
  page:       1,
  pageSize:   5,
  totalPages: 1,
}

vi.mock('@/features/dashboard/api/useDashboard', () => ({
  useDashboardSummary:   () => ({ data: mockSummary,            isLoading: false }),
  useBalanceEvolution:   () => ({ data: mockBalanceEvolution,   isLoading: false }),
  useExpensesByCategory: () => ({ data: mockExpensesByCategory, isLoading: false }),
  useWeeklyComparison:   () => ({ data: mockWeeklyComparison,   isLoading: false }),
}))

vi.mock('@/features/budgets/api/useBudgets', () => ({
  useBudgetSummary: () => ({ data: mockBudgetSummaries, isLoading: false }),
}))

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useTransactions: () => ({ data: mockTransactions, isLoading: false }),
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

  it('deve exibir o saldo projetado', () => {
    renderPage()
    expect(screen.getByText('R$ 2.300,00')).toBeInTheDocument()
  })

  it('deve exibir o seletor de mês/ano', () => {
    renderPage()
    const now        = new Date()
    const monthLabel = now.toLocaleString('pt-BR', { month: 'long', year: 'numeric' })
    expect(screen.getByText(new RegExp(monthLabel, 'i'))).toBeInTheDocument()
  })

  it('deve exibir o widget de últimas transações', () => {
    renderPage()
    expect(screen.getByText('Últimas Transações')).toBeInTheDocument()
    expect(screen.getByText('Salário')).toBeInTheDocument()
    expect(screen.getByText('Mercado')).toBeInTheDocument()
  })

  it('deve exibir o widget de top orçamentos', () => {
    renderPage()
    expect(screen.getByText('Top Orçamentos')).toBeInTheDocument()
    expect(screen.getByText('Alimentação')).toBeInTheDocument()
  })

  it('deve exibir os gráficos', () => {
    renderPage()
    expect(screen.getByText('Evolução do Saldo')).toBeInTheDocument()
    expect(screen.getByText('Despesas por Categoria')).toBeInTheDocument()
    expect(screen.getByText('Comparação Semanal')).toBeInTheDocument()
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