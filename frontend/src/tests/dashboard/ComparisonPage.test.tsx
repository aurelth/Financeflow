import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ComparisonPage from '@/features/dashboard/pages/ComparisonPage'

const mockPeriodComparison = {
  periods: [
    { month: 2, year: 2026, totalIncome: 4500, totalExpenses: 2500, balance: 2000 },
    { month: 3, year: 2026, totalIncome: 5000, totalExpenses: 3000, balance: 2000 },
  ],
  categoryComparisons: [
    {
      categoryId:    'cat-1',
      categoryName:  'Alimentação',
      categoryIcon:  '🍔',
      categoryColor: '#f97316',
      values:        [1000, 1500],
      variations:    [null, 50],
    },
    {
      categoryId:    'cat-2',
      categoryName:  'Transporte',
      categoryIcon:  '🚗',
      categoryColor: '#6366f1',
      values:        [1500, 1500],
      variations:    [null, 0],
    },
  ],
}

vi.mock('@/features/dashboard/api/useDashboard', () => ({
  usePeriodComparison: () => ({ data: mockPeriodComparison, isLoading: false }),
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ComparisonPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('ComparisonPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Comparativo Histórico')
  })

  it('deve exibir o seletor de períodos', () => {
    renderPage()
    expect(screen.getByText('Períodos selecionados')).toBeInTheDocument()
  })

  it('deve exibir os cards de sumário por período', () => {
    renderPage()
    expect(screen.getByText('Fev 2026')).toBeInTheDocument()
    expect(screen.getByText('Mar 2026')).toBeInTheDocument()
  })

  it('deve exibir os valores de receita e despesa por período', () => {
    renderPage()
    expect(screen.getByText('R$ 4.500,00')).toBeInTheDocument()
    expect(screen.getByText('R$ 5.000,00')).toBeInTheDocument()
  })

  it('deve exibir os títulos dos gráficos', () => {
    renderPage()
    expect(screen.getByText('Evolução entre Períodos')).toBeInTheDocument()
    expect(screen.getByText('Despesas por Categoria')).toBeInTheDocument()
  })

  it('deve exibir a tabela de detalhamento', () => {
    renderPage()
    expect(screen.getByText('Detalhamento por Categoria')).toBeInTheDocument()
    expect(screen.getByText('Alimentação')).toBeInTheDocument()
    expect(screen.getByText('Transporte')).toBeInTheDocument()
  })

  it('deve exibir botão de adicionar período', () => {
    renderPage()
    expect(screen.getByText('Adicionar período')).toBeInTheDocument()
  })

  it('deve adicionar período ao clicar em adicionar', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByText('Adicionar período'))

    await waitFor(() => {
      expect(screen.queryByText('Adicionar período')).not.toBeInTheDocument()
    })
  })
})