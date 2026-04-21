import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import GoalsPage from '@/features/goals/pages/GoalsPage'

const mockCreateGoal = vi.fn()
const mockUpdateGoal = vi.fn()
const mockDeleteGoal = vi.fn()

vi.mock('@/features/goals/api/useGoals', () => ({
    useGoals: () => ({
        data: {
            availableThisMonth: 1500,
            committedThisMonth: 500,
            difference: 1000,
            goals: [
                {
                    id: 'goal-1',
                    name: 'Viagem para Europa',
                    emoji: '✈️',
                    targetAmount: 10000,
                    monthlyContribution: 500,
                    deadline: '2026-12-31T00:00:00',
                    accumulatedAmount: 2500,
                    plannedThisMonth: 500,
                    receivedThisMonth: 500,
                    progressPercentage: 25,
                    isCompleted: false,
                    monthsToComplete: 15,
                    status: 'OnTrack',
                },
                {
                    id: 'goal-2',
                    name: 'Fundo de emergência',
                    emoji: '🛡️',
                    targetAmount: 5000,
                    monthlyContribution: 300,
                    deadline: '2026-06-30T00:00:00',
                    accumulatedAmount: 5000,
                    plannedThisMonth: 0,
                    receivedThisMonth: 0,
                    progressPercentage: 100,
                    isCompleted: true,
                    monthsToComplete: null,
                    status: 'Completed',
                },
            ],
        },
        isLoading: false,
    }),
    useCreateGoal: () => ({ mutate: mockCreateGoal, isPending: false }),
    useUpdateGoal: () => ({ mutate: mockUpdateGoal, isPending: false }),
    useDeleteGoal: () => ({ mutate: mockDeleteGoal, isPending: false }),
}))

const renderPage = () => {
    const qc = new QueryClient()
    return render(
        <QueryClientProvider client={qc}>
            <MemoryRouter>
                <GoalsPage />
            </MemoryRouter>
        </QueryClientProvider>
    )
}

describe('GoalsPage', () => {
    beforeEach(() => vi.clearAllMocks())

    it('deve renderizar o título da página', () => {
        renderPage()
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Metas Financeiras')
    })

    it('deve exibir o resumo do mês quando há metas', () => {
        renderPage()
        expect(screen.getByText('Poupança disponível')).toBeInTheDocument()
        expect(screen.getByText('Comprometido com metas')).toBeInTheDocument()
        expect(screen.getByText('Diferença')).toBeInTheDocument()
    })

    it('deve exibir metas ativas', () => {
        renderPage()
        expect(screen.getByText('Viagem para Europa')).toBeInTheDocument()
    })

    it('deve exibir metas concluídas', () => {
        renderPage()
        expect(screen.getByText('Fundo de emergência')).toBeInTheDocument()
    })

    it('deve exibir secção "Em andamento"', () => {
        renderPage()
        expect(screen.getByText(/Em andamento/i)).toBeInTheDocument()
    })

    it('deve exibir secção "Concluídas"', () => {
        renderPage()
        expect(screen.getByText(/Concluídas/i)).toBeInTheDocument()
    })

    it('deve abrir modal de criação ao clicar em nova meta', async () => {
        renderPage()
        const user = userEvent.setup()

        const buttons = screen.getAllByRole('button', { name: /nova meta/i })
        await user.click(buttons[0])

        await waitFor(() => {
            const titles = screen.getAllByText('Nova meta')
            expect(titles.length).toBeGreaterThanOrEqual(2)
        })
    })

    it('deve abrir modal de edição ao clicar em editar', async () => {
        renderPage()
        const user = userEvent.setup()

        const editButtons = screen.getAllByTitle('Editar')
        await user.click(editButtons[0])

        await waitFor(() => {
            expect(screen.getByText('Editar meta')).toBeInTheDocument()
        })
    })

    it('deve abrir modal de remoção ao clicar em remover', async () => {
        renderPage()
        const user = userEvent.setup()

        const deleteButtons = screen.getAllByTitle('Remover')
        await user.click(deleteButtons[0])

        await waitFor(() => {
            const elements = screen.getAllByText('Remover meta')
            expect(elements.length).toBeGreaterThanOrEqual(1)
        })
    })

    it('deve exibir o progresso percentual da meta', () => {
        renderPage()
        expect(screen.getByText('25%')).toBeInTheDocument()
    })

    it('deve exibir o status da meta ativa', () => {
        renderPage()
        expect(screen.getByText('Em dia')).toBeInTheDocument()
    })

    it('deve exibir o status da meta concluída', () => {
        renderPage()
        expect(screen.getByText('Concluída')).toBeInTheDocument()
    })
})