import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ImportsPreviewPage from '@/features/imports/pages/ImportsPreviewPage'
import { TransactionType } from '@/features/categories/types/category.types'
import type { BankImportPreviewDto } from '@/features/imports/types/imports.types'

const mockNavigate = vi.fn()

vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual('react-router-dom')
    return { ...actual, useNavigate: () => mockNavigate }
})

const mockConfirmMutate = vi.fn()

vi.mock('@/features/imports/api/useImports', () => ({
    useImportPreview: () => ({ data: mockPreview, isLoading: false }),
    useConfirmImport: () => ({ mutate: mockConfirmMutate, isPending: false }),
}))

vi.mock('@/features/categories/api/useCategories', () => ({
    useCategories: () => ({ data: mockCategories }),
}))

const mockCategories = [
    {
        id: 'cat-1',
        name: 'Alimentação',
        icon: 'utensils',
        color: '#f59e0b',
        type: TransactionType.Expense,
        isDefault: true,
        isActive: true,
        isOwner: false,
        subcategories: [],
    },
    {
        id: 'cat-2',
        name: 'Salário',
        icon: 'briefcase',
        color: '#22c55e',
        type: TransactionType.Income,
        isDefault: true,
        isActive: true,
        isOwner: false,
        subcategories: [],
    },
]

const mockPreview: BankImportPreviewDto = {
    importId: 'imp-1',
    fileName: 'extrato-janeiro.ofx',
    status: 'Pending',
    totalRecords: 3,
    transactions: [
        {
            id: 'bt-1',
            externalId: 'FIT001',
            date: '2026-01-15T00:00:00',
            amount: 150.00,
            description: 'IFOOD RESTAURANTE',
            type: 'Expense',
            hash: 'abc123',
            suggestedCategoryId: 'cat-1',
            isDuplicate: false,
            isSelected: true,
            transactionId: null,
        },
        {
            id: 'bt-2',
            externalId: 'FIT002',
            date: '2026-01-20T00:00:00',
            amount: 5000.00,
            description: 'SALARIO EMPRESA',
            type: 'Income',
            hash: 'def456',
            suggestedCategoryId: 'cat-2',
            isDuplicate: false,
            isSelected: true,
            transactionId: null,
        },
        {
            id: 'bt-3',
            externalId: 'FIT003',
            date: '2026-01-22T00:00:00',
            amount: 80.00,
            description: 'UBER TRIP',
            type: 'Expense',
            hash: 'ghi789',
            suggestedCategoryId: null,
            isDuplicate: true,
            isSelected: false,
            transactionId: 'tx-existing',
        },
    ],
}

const renderPage = () => {
    const qc = new QueryClient()
    return render(
        <QueryClientProvider client={qc}>
            <MemoryRouter initialEntries={['/imports/imp-1/preview']}>
                <Routes>
                    <Route path="/imports/:id/preview" element={<ImportsPreviewPage />} />
                </Routes>
            </MemoryRouter>
        </QueryClientProvider>
    )
}

describe('ImportsPreviewPage', () => {
    beforeEach(() => vi.clearAllMocks())

    it('deve renderizar o título da página', () => {
        renderPage()
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Preview da importação')
    })

    it('deve exibir o nome do ficheiro', () => {
        renderPage()
        expect(screen.getAllByText('extrato-janeiro.ofx').length).toBeGreaterThan(0)
    })

    it('deve exibir o total de transações', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText(/3 transações encontradas/)).toBeInTheDocument()
        })
    })

    it('deve exibir as descrições das transações', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText('IFOOD RESTAURANTE')).toBeInTheDocument()
            expect(screen.getByText('SALARIO EMPRESA')).toBeInTheDocument()
            expect(screen.getByText('UBER TRIP')).toBeInTheDocument()
        })
    })

    it('deve exibir badge de duplicada para transações duplicadas', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText('duplicada')).toBeInTheDocument()
        })
    })

    it('deve exibir aviso de duplicadas detetadas no resumo', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText(/1 duplicada detetada/)).toBeInTheDocument()
        })
    })

    it('deve exibir botão de confirmar com contagem de selecionadas', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByRole('button', { name: /confirmar/i })).toBeInTheDocument()
        })
    })

    it('deve navegar de volta ao clicar na seta', async () => {
        renderPage()
        const user = userEvent.setup()

        const backButton = screen.getAllByRole('button')[0]
        await user.click(backButton)

        expect(mockNavigate).toHaveBeenCalledWith('/imports')
    })

    it('deve chamar confirm ao clicar em confirmar', async () => {
        renderPage()
        const user = userEvent.setup()

        await waitFor(() => {
            expect(screen.getByRole('button', { name: /confirmar/i })).not.toBeDisabled()
        })

        await user.click(screen.getByRole('button', { name: /confirmar/i }))

        expect(mockConfirmMutate).toHaveBeenCalledWith(
            { selectedTransactionIds: ['bt-1', 'bt-2'] },
            expect.any(Object)
        )
    })

    it('deve exibir 2 selecionadas de 3 no resumo da tabela', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText('2', { selector: 'span' })).toBeInTheDocument()
            expect(screen.getByText('3', { selector: 'span' })).toBeInTheDocument()
        })
    })

    it('deve desmarcar transação ao clicar no checkbox', async () => {
        renderPage()
        const user = userEvent.setup()

        await waitFor(() => {
            expect(screen.getByText('IFOOD RESTAURANTE')).toBeInTheDocument()
        })
        
        // Os checkboxes de linha começam depois do "Selecionar todas"
        const checkboxes = screen.getAllByRole('checkbox') as HTMLInputElement[]
        // O "Selecionar todas" é o último do header — os de linha são os não-disabled excluindo o primeiro
        const rowCheckboxes = checkboxes.filter(cb => !cb.disabled)
        // Clica no segundo checkbox não-disabled (o primeiro é o "Selecionar todas")
        await user.click(rowCheckboxes[1])

        await waitFor(() => {
            const summary = screen.getByText(/transações selecionadas/)
            expect(summary.textContent).toMatch(/1\s*de\s*3/)
        })
    })

    it('deve ter checkbox de duplicada desabilitado', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText('duplicada')).toBeInTheDocument()
        })
        const checkboxes = screen.getAllByRole('checkbox') as HTMLInputElement[]
        const disabledCheckboxes = checkboxes.filter(cb => cb.disabled)
        expect(disabledCheckboxes.length).toBeGreaterThan(0)
    })

    it('deve exibir select de categoria preenchido para transações com sugestão', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText('IFOOD RESTAURANTE')).toBeInTheDocument()
        })
        const selects = screen.getAllByRole('combobox') as HTMLSelectElement[]
        const withValue = selects.filter(s => s.value === 'cat-1' || s.value === 'cat-2')
        expect(withValue.length).toBeGreaterThan(0)
    })
})