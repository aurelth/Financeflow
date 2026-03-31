import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import PeriodSummaryCards from '@/features/dashboard/components/PeriodSummaryCards'
import type { PeriodData } from '@/features/dashboard/types/dashboard.types'

const mockPeriods: PeriodData[] = [
  { month: 2, year: 2026, totalIncome: 4500, totalExpenses: 2500, balance: 2000 },
  { month: 3, year: 2026, totalIncome: 5000, totalExpenses: 3000, balance: 2000 },
]

const renderComponent = (periods: PeriodData[]) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <PeriodSummaryCards periods={periods} />
    </QueryClientProvider>
  )
}

describe('PeriodSummaryCards', () => {
  it('deve renderizar um card por período', () => {
    renderComponent(mockPeriods)
    expect(screen.getByText('Fev 2026')).toBeInTheDocument()
    expect(screen.getByText('Mar 2026')).toBeInTheDocument()
  })

  it('deve exibir receitas, despesas e saldo por período', () => {
    renderComponent(mockPeriods)
    expect(screen.getByText('R$ 4.500,00')).toBeInTheDocument()
    expect(screen.getByText('R$ 5.000,00')).toBeInTheDocument()
  })

  it('deve exibir variação percentual para o segundo período', () => {
    renderComponent(mockPeriods)
    // Receitas: (5000-4500)/4500 * 100 = 11.1%
    expect(screen.getByText('11.1%')).toBeInTheDocument()
  })

  it('não deve exibir variação no primeiro período', () => {
    renderComponent([mockPeriods[0]])
    expect(screen.queryByText('%')).not.toBeInTheDocument()
  })
})