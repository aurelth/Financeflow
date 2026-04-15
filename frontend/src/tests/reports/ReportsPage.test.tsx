import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ReportsPage from '@/features/reports/pages/ReportsPage'

// ── Mocks de dados ────────────────────────────────────────────────────────────

const mockCashFlow = {
    from: '2026-01-01', to: '2026-03-31', groupBy: 'month',
    totalIncome: 15000, totalExpenses: 9000, netBalance: 6000,
    periods: [
        { label: 'jan/2026', income: 5000, expenses: 3000, balance: 2000, cumulativeBalance: 2000 },
        { label: 'fev/2026', income: 5000, expenses: 3000, balance: 2000, cumulativeBalance: 4000 },
        { label: 'mar/2026', income: 5000, expenses: 3000, balance: 2000, cumulativeBalance: 6000 },
    ],
}

const mockAnnualSummary = {
    year: 2026, totalIncome: 60000, totalExpenses: 36000, netBalance: 24000,
    averageMonthlyIncome: 5000, averageMonthlyExpenses: 3000,
    months: Array.from({ length: 12 }, (_, i) => ({
        month: i + 1,
        monthName: ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'][i],
        income: 5000, expenses: 3000, balance: 2000,
        cumulativeBalance: (i + 1) * 2000,
    })),
}

const mockByCategory = {
    from: '2026-01-01', to: '2026-03-31',
    totalExpenses: 9000, totalIncome: 15000,
    categories: [
        {
            categoryId: 'cat-1', categoryName: 'Alimentação', categoryIcon: '🍔',
            categoryColor: '#f97316', type: 'Expense' as const,
            amount: 4500, percentage: 50, transactionCount: 10,
            subcategories: [],
        },
        {
            categoryId: 'cat-2', categoryName: 'Transporte', categoryIcon: '🚗',
            categoryColor: '#6366f1', type: 'Expense' as const,
            amount: 4500, percentage: 50, transactionCount: 8,
            subcategories: [],
        },
    ],
}

const mockByTag = {
    from: '2026-01-01', to: '2026-03-31', totalAmount: 3000,
    tags: [
        { tag: 'alimentação', amount: 2000, percentage: 66.7, transactionCount: 5 },
        { tag: 'transporte', amount: 1000, percentage: 33.3, transactionCount: 3 },
    ],
}

const mockProjections = {
    monthsAnalysed: 12, monthsAhead: 3,
    historical: Array.from({ length: 12 }, (_, i) => ({
        year: 2026, month: i + 1,
        monthName: ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'][i],
        income: 5000, expenses: 3000, balance: 2000, isProjected: false,
    })),
    projected: [
        { year: 2027, month: 1, monthName: 'Janeiro', income: 5100, expenses: 3050, balance: 2050, isProjected: true },
        { year: 2027, month: 2, monthName: 'Fevereiro', income: 5150, expenses: 3080, balance: 2070, isProjected: true },
        { year: 2027, month: 3, monthName: 'Março', income: 5200, expenses: 3100, balance: 2100, isProjected: true },
    ],
}

vi.mock('@/features/reports/api/useAnalytics', () => ({
    useCashFlow: () => ({ data: mockCashFlow, isLoading: false }),
    useAnnualSummary: () => ({ data: mockAnnualSummary, isLoading: false }),
    useReportByCategory: () => ({ data: mockByCategory, isLoading: false }),
    useReportByTag: () => ({ data: mockByTag, isLoading: false }),
    useProjections: () => ({ data: mockProjections, isLoading: false }),
}))

// ── Helper ────────────────────────────────────────────────────────────────────

const renderPage = () => {
    const qc = new QueryClient()
    return render(
        <QueryClientProvider client={qc}>
            <MemoryRouter>
                <ReportsPage />
            </MemoryRouter>
        </QueryClientProvider>
    )
}

// ── Testes ────────────────────────────────────────────────────────────────────

describe('ReportsPage', () => {
    beforeEach(() => vi.clearAllMocks())

    it('deve renderizar o título da página', () => {
        renderPage()
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Relatórios')
    })

    it('deve exibir as 5 tabs de navegação', () => {
        renderPage()
        expect(screen.getAllByText('Fluxo de Caixa').length).toBeGreaterThanOrEqual(1)
        expect(screen.getByText('Resumo Anual')).toBeInTheDocument()
        expect(screen.getByText('Por Categoria')).toBeInTheDocument()
        expect(screen.getByText('Por Tag')).toBeInTheDocument()
        expect(screen.getByText('Projecções')).toBeInTheDocument()
    })

    it('deve mostrar a tab Fluxo de Caixa por defeito', () => {
        renderPage()        
        expect(screen.getAllByText('Fluxo de Caixa').length).toBeGreaterThanOrEqual(1)
        expect(screen.getByText('R$ 15.000,00')).toBeInTheDocument()
    })

    it('deve exibir filtros de período na tab Fluxo de Caixa', () => {
        renderPage()
        expect(screen.getByText('De')).toBeInTheDocument()
        expect(screen.getByText('Até')).toBeInTheDocument()
    })

    it('deve exibir toggle de agrupamento na tab Fluxo de Caixa', () => {
        renderPage()
        expect(screen.getByText('Dia')).toBeInTheDocument()
        expect(screen.getByText('Mês')).toBeInTheDocument()
    })

    it('deve navegar para tab Resumo Anual ao clicar', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByText('Resumo Anual'))

        await waitFor(() => {
            expect(screen.getByText('2026')).toBeInTheDocument()
        })
    })

    it('deve exibir seletor de ano na tab Resumo Anual', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByText('Resumo Anual'))

        await waitFor(() => {
            const chevrons = document.querySelectorAll('.lucide-chevron-left, .lucide-chevron-right')
            expect(chevrons.length).toBeGreaterThan(0)
        })
    })

    it('deve navegar para tab Por Categoria ao clicar', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByText('Por Categoria'))

        await waitFor(() => {
            expect(screen.getByText('Total Despesas')).toBeInTheDocument()
            expect(screen.getByText('Total Receitas')).toBeInTheDocument()
        })
    })

    it('deve exibir toggle de tipo na tab Por Categoria', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByText('Por Categoria'))

        await waitFor(() => {
            expect(screen.getByText('Todos')).toBeInTheDocument()
            expect(screen.getByText('Despesas')).toBeInTheDocument()
            expect(screen.getByText('Receitas')).toBeInTheDocument()
        })
    })

    it('deve navegar para tab Por Tag ao clicar', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByText('Por Tag'))

        await waitFor(() => {
            expect(screen.getByText('alimentação')).toBeInTheDocument()
            expect(screen.getByText('transporte')).toBeInTheDocument()
        })
    })

    it('deve navegar para tab Projecções ao clicar', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByText('Projecções'))

        await waitFor(() => {
            expect(screen.getByText('Meses analisados')).toBeInTheDocument()
            expect(screen.getByText('Meses projectados')).toBeInTheDocument()
        })
    })

    it('deve exibir nota explicativa das projecções', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByText('Projecções'))

        await waitFor(() => {
            expect(screen.getByText(/regressão linear ponderada/i)).toBeInTheDocument()
        })
    })

    it('deve exibir totais do fluxo de caixa', () => {
        renderPage()
        expect(screen.getByText('Total Receitas')).toBeInTheDocument()
        expect(screen.getByText('Total Despesas')).toBeInTheDocument()
        expect(screen.getByText('Saldo Líquido')).toBeInTheDocument()
    })

    it('deve não exibir filtros de período na tab Resumo Anual', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByText('Resumo Anual'))

        await waitFor(() => {
            expect(screen.queryByText('De')).not.toBeInTheDocument()
            expect(screen.queryByText('Até')).not.toBeInTheDocument()
        })
    })

    it('deve não exibir filtros de período na tab Projecções', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByText('Projecções'))

        await waitFor(() => {
            expect(screen.queryByText('De')).not.toBeInTheDocument()
            expect(screen.queryByText('Até')).not.toBeInTheDocument()
        })
    })
})