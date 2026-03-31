import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ComparisonTable from '@/features/dashboard/components/ComparisonTable'
import type { CategoryComparison, PeriodData } from '@/features/dashboard/types/dashboard.types'

const mockPeriods: PeriodData[] = [
  { month: 2, year: 2026, totalIncome: 4500, totalExpenses: 2500, balance: 2000 },
  { month: 3, year: 2026, totalIncome: 5000, totalExpenses: 3000, balance: 2000 },
]

const mockCategories: CategoryComparison[] = [
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
    values:        [800, 600],
    variations:    [null, -25],
  },
]

const renderComponent = (categories: CategoryComparison[], periods: PeriodData[]) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <ComparisonTable categories={categories} periods={periods} />
    </QueryClientProvider>
  )
}

describe('ComparisonTable', () => {
  it('deve renderizar o título', () => {
    renderComponent(mockCategories, mockPeriods)
    expect(screen.getByText('Detalhamento por Categoria')).toBeInTheDocument()
  })

  it('deve exibir as categorias', () => {
    renderComponent(mockCategories, mockPeriods)
    expect(screen.getByText('Alimentação')).toBeInTheDocument()
    expect(screen.getByText('Transporte')).toBeInTheDocument()
  })

  it('deve exibir os valores por período', () => {
    renderComponent(mockCategories, mockPeriods)
    expect(screen.getByText('R$ 1.000,00')).toBeInTheDocument()
    expect(screen.getByText('R$ 1.500,00')).toBeInTheDocument()
  })

  it('deve exibir estado vazio quando não há categorias', () => {
    renderComponent([], mockPeriods)
    expect(screen.getByText(/nenhuma despesa/i)).toBeInTheDocument()
  })

  it('deve exibir delta positivo para aumento de gastos', () => {
    renderComponent(mockCategories, mockPeriods)
    // Alimentação: 1500 - 1000 = +500
    expect(screen.getByText('+R$ 500,00')).toBeInTheDocument()
  })

  it('deve exibir delta negativo para redução de gastos', () => {
    renderComponent(mockCategories, mockPeriods)
    // Transporte: 600 - 800 = -200
    expect(screen.getByText('-R$ 200,00')).toBeInTheDocument()
  })
})