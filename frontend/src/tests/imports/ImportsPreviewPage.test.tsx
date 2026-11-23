import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ImportsPreviewPage from '@/features/imports/pages/ImportsPreviewPage'
import type { BankImportPreviewDto } from '@/features/imports/types/imports.types'

const mockNavigate = vi.fn()

vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual('react-router-dom')
    return { ...actual, useNavigate: () => mockNavigate }
})

const mockToastError = vi.fn()

vi.mock('sonner', () => ({
    toast: {
        error: (...args: unknown[]) => mockToastError(...args),
        success: vi.fn(),
        warning: vi.fn(),
    },
}))

const mockConfirmMutate = vi.fn()
const mockChangeAccountMutate = vi.fn()

vi.mock('@/features/imports/api/useImports', () => ({
    useImportPreview: () => ({ data: mockPreview, isLoading: false }),
    useConfirmImport: () => ({ mutate: mockConfirmMutate, isPending: false }),
    useChangeImportAccount: () => ({ mutate: mockChangeAccountMutate, isPending: false }),
}))

vi.mock('@/features/categories/api/useCategories', () => ({
    useCategories: () => ({ data: mockCategories }),
    useGoalCategories: () => ({ data: [] }),
}))

vi.mock('@/features/accounts/api/useAccounts', () => ({
    useAccounts: () => ({
        data: [
            { id: 'acc-1', name: 'Nubank', isArchived: false },
            { id: 'acc-2', name: 'Itaú', isArchived: false },
        ],
    }),
}))

const mockCategories = [
    {
        id: 'cat-1',
        name: 'Alimentação',
        icon: 'utensils',
        color: '#f59e0b',
        type: 2,
        isDefault: true,
        isActive: true,
        isOwner: false,
        isGoalCategory: false,
        subcategories: [],
    },
    {
        id: 'cat-2',
        name: 'Salário',
        icon: 'briefcase',
        color: '#22c55e',
        type: 1,
        isDefault: true,
        isActive: true,
        isOwner: false,
        isGoalCategory: false,
        subcategories: [],
    },
]

const basePreview: BankImportPreviewDto = {
    importId: 'imp-1',
    fileName: 'extrato-janeiro.ofx',
    status: 'Completed',
    totalRecords: 3,
    duplicates: 1,
    accountId: 'acc-1',
    accountName: 'Nubank',
    transactions: [
        {
            id: 'bt-1',
            externalId: 'FIT001',
            date: '2026-01-15T00:00:00',
            amount: 150.0,
            description: 'IFOOD RESTAURANTE',
            type: 'Expense',
            suggestedCategoryId: 'cat-1',
            isDuplicate: false,
            isSelected: true,
        },
        {
            id: 'bt-2',
            externalId: 'FIT002',
            date: '2026-01-20T00:00:00',
            amount: 5000.0,
            description: 'SALARIO EMPRESA',
            type: 'Income',
            suggestedCategoryId: 'cat-2',
            isDuplicate: false,
            isSelected: true,
        },
        {
            id: 'bt-3',
            externalId: 'FIT003',
            date: '2026-01-22T00:00:00',
            amount: 80.0,
            description: 'UBER TRIP',
            type: 'Expense',
            suggestedCategoryId: null,
            isDuplicate: true,
            isSelected: false,
            duplicateReason: 'SameFitId',
            match: {
                transactionId: 'tx-existing',
                date: '2026-01-22T00:00:00',
                amount: 80.0,
                description: 'UBER ANTERIOR',
            },
        },
    ],
}

let mockPreview: BankImportPreviewDto = basePreview

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

const withTransactions = (extra: BankImportPreviewDto['transactions']) => ({
    ...basePreview,
    transactions: [...basePreview.transactions, ...extra],
})

describe('ImportsPreviewPage', () => {
    beforeEach(() => {
        vi.clearAllMocks()
        mockPreview = basePreview
    })

    it('deve renderizar o título da página', () => {
        renderPage()
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Preview da importação')
    })

    it('deve exibir o nome do arquivo', () => {
        renderPage()
        expect(screen.getAllByText('extrato-janeiro.ofx').length).toBeGreaterThan(0)
    })

    it('deve exibir o total de transações', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText(/3 transações encontradas/)).toBeInTheDocument()
        })
    })

    it('deve exibir as descrições das transações novas', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText('IFOOD RESTAURANTE')).toBeInTheDocument()
            expect(screen.getByText('SALARIO EMPRESA')).toBeInTheDocument()
        })
    })

    it('deve exibir aviso de duplicadas detetadas no resumo', async () => {
        renderPage()
        await waitFor(() => {
            expect(screen.getByText(/1 duplicada detetada/)).toBeInTheDocument()
        })
    })

    it('deve navegar de volta ao clicar na seta', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByRole('button', { name: 'Voltar' }))

        expect(mockNavigate).toHaveBeenCalledWith('/imports')
    })

    it('deve exibir 2 de 2 selecionadas, sem contar as duplicadas', async () => {
        renderPage()
        await waitFor(() => {
            const summary = screen.getByText(/transações selecionadas/)
            expect(summary.textContent).toMatch(/2\s*de\s*2/)
        })
    })

    it('deve desmarcar transação ao clicar no checkbox', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByRole('checkbox', { name: 'Selecionar IFOOD RESTAURANTE' }))

        await waitFor(() => {
            const summary = screen.getByText(/transações selecionadas/)
            expect(summary.textContent).toMatch(/1\s*de\s*2/)
        })
    })

    it('deve exibir select de categoria preenchido para transações com sugestão', async () => {
        renderPage()
        const select = screen.getByRole('combobox', { name: 'Categoria de IFOOD RESTAURANTE' }) as HTMLSelectElement
        expect(select.value).toBe('cat-1')
    })

    describe('confirmação', () => {
        it('deve chamar confirm só com as linhas novas selecionadas', async () => {
            renderPage()
            const user = userEvent.setup()

            await waitFor(() => {
                expect(screen.getByRole('button', { name: /confirmar/i })).not.toBeDisabled()
            })

            await user.click(screen.getByRole('button', { name: /confirmar/i }))

            expect(mockConfirmMutate).toHaveBeenCalledWith(
                {
                    transactions: [
                        { id: 'bt-1', isSelected: true, categoryId: 'cat-1', type: 'Expense' },
                        { id: 'bt-2', isSelected: true, categoryId: 'cat-2', type: 'Income' },
                    ],
                },
                expect.any(Object)
            )
        })

        it('deve destacar as linhas recusadas pela API', async () => {
            mockConfirmMutate.mockImplementation((_body, options) =>
                options.onError({
                    isAxiosError: true,
                    response: { data: { errors: { 'bt-1': ['InvalidCategory'] } } },
                })
            )
            renderPage()
            const user = userEvent.setup()

            await waitFor(() => {
                expect(screen.getByRole('button', { name: /confirmar/i })).not.toBeDisabled()
            })
            await user.click(screen.getByRole('button', { name: /confirmar/i }))

            await waitFor(() => {
                expect(screen.getByTestId('import-row-bt-1')).toHaveAttribute('data-error', 'true')
            })
            expect(screen.getByTestId('import-row-bt-2')).not.toHaveAttribute('data-error')
            expect(mockToastError).toHaveBeenCalled()
        })
    })

    describe('duplicadas', () => {
        it('deve manter as duplicadas ocultas até o usuário revisar', async () => {
            renderPage()

            await waitFor(() => {
                expect(screen.getByText(/1 possível duplicada/)).toBeInTheDocument()
            })
            expect(screen.queryByText('UBER TRIP')).not.toBeInTheDocument()
        })

        it('deve listar a duplicada com o motivo e o lançamento existente', async () => {
            renderPage()
            const user = userEvent.setup()

            await user.click(screen.getByRole('button', { name: /possível duplicada/ }))

            expect(screen.getByText('UBER TRIP')).toBeInTheDocument()
            expect(screen.getByText('duplicada')).toBeInTheDocument()
            expect(screen.getByText(/mesmo identificador do banco/)).toBeInTheDocument()
            expect(screen.getByText(/UBER ANTERIOR/)).toBeInTheDocument()
        })

        it('deve diferenciar a possível duplicata de lançamento manual', async () => {
            mockPreview = withTransactions([
                {
                    id: 'bt-4',
                    externalId: '',
                    date: '2026-01-23T00:00:00',
                    amount: 30.0,
                    description: 'MERCADO',
                    type: 'Expense',
                    suggestedCategoryId: null,
                    isDuplicate: true,
                    isSelected: false,
                    duplicateReason: 'PossibleManual',
                    match: {
                        transactionId: 'tx-manual',
                        date: '2026-01-22T00:00:00',
                        amount: 30.0,
                        description: 'Mercado do bairro',
                    },
                },
            ])
            renderPage()
            const user = userEvent.setup()

            await user.click(screen.getByRole('button', { name: /possíveis duplicadas|possível duplicada/ }))

            expect(screen.getByText('possível duplicata')).toBeInTheDocument()
            expect(screen.getByText(/digitou manualmente/)).toBeInTheDocument()
        })

        it('deve começar com a duplicada desmarcada', async () => {
            renderPage()
            const user = userEvent.setup()

            await user.click(screen.getByRole('button', { name: /possível duplicada/ }))

            expect(
                screen.getByRole('checkbox', { name: 'Importar mesmo assim: UBER TRIP' })
            ).not.toBeChecked()
        })

        it('deve enviar importAnyway apenas para a duplicada liberada pelo usuário', async () => {
            renderPage()
            const user = userEvent.setup()

            await user.click(screen.getByRole('button', { name: /possível duplicada/ }))
            await user.click(screen.getByRole('checkbox', { name: 'Importar mesmo assim: UBER TRIP' }))
            await user.selectOptions(
                screen.getByRole('combobox', { name: 'Categoria de UBER TRIP' }),
                'cat-1'
            )
            await user.click(screen.getByRole('button', { name: /confirmar/i }))

            expect(mockConfirmMutate).toHaveBeenCalledWith(
                {
                    transactions: [
                        { id: 'bt-1', isSelected: true, categoryId: 'cat-1', type: 'Expense' },
                        { id: 'bt-2', isSelected: true, categoryId: 'cat-2', type: 'Income' },
                        { id: 'bt-3', isSelected: true, categoryId: 'cat-1', type: 'Expense', importAnyway: true },
                    ],
                },
                expect.any(Object)
            )
        })

        it('não deve selecionar a duplicada ao usar "Selecionar todas"', async () => {
            renderPage()
            const user = userEvent.setup()

            await user.click(screen.getByRole('button', { name: /possível duplicada/ }))
            const selectAll = screen.getByRole('checkbox', { name: /selecionar todas/i })
            await user.click(selectAll) // desmarca
            await user.click(selectAll) // marca de novo

            expect(
                screen.getByRole('checkbox', { name: 'Importar mesmo assim: UBER TRIP' })
            ).not.toBeChecked()
        })
    })

    describe('linhas sem categoria', () => {
        const semCategoria = {
            id: 'bt-5',
            externalId: 'FIT005',
            date: '2026-01-25T00:00:00',
            amount: 45.0,
            description: 'LOJA SEM CATEGORIA',
            type: 'Expense' as const,
            suggestedCategoryId: null,
            isDuplicate: false,
            isSelected: true,
        }

        it('deve bloquear a confirmação e destacar a linha', async () => {
            mockPreview = withTransactions([semCategoria])
            renderPage()

            await waitFor(() => {
                expect(screen.getByRole('button', { name: /confirmar/i })).toBeDisabled()
            })
            expect(screen.getByText(/1 lançamento sem categoria/)).toBeInTheDocument()
            expect(screen.getByTestId('import-row-bt-5')).toHaveAttribute('data-error', 'true')
            expect(screen.getByTestId('import-row-bt-1')).not.toHaveAttribute('data-error')
        })

        it('deve liberar a confirmação ao escolher a categoria', async () => {
            mockPreview = withTransactions([semCategoria])
            renderPage()
            const user = userEvent.setup()

            await user.selectOptions(
                screen.getByRole('combobox', { name: 'Categoria de LOJA SEM CATEGORIA' }),
                'cat-1'
            )

            await waitFor(() => {
                expect(screen.getByRole('button', { name: /confirmar/i })).not.toBeDisabled()
            })
            expect(screen.queryByText(/sem categoria\. Escolha/)).not.toBeInTheDocument()
        })

        it('deve liberar a confirmação ao desmarcar a linha', async () => {
            mockPreview = withTransactions([semCategoria])
            renderPage()
            const user = userEvent.setup()

            await user.click(screen.getByRole('checkbox', { name: 'Selecionar LOJA SEM CATEGORIA' }))

            await waitFor(() => {
                expect(screen.getByRole('button', { name: /confirmar/i })).not.toBeDisabled()
            })
        })

        it('deve desmarcar todas as linhas sem categoria pelo atalho do aviso', async () => {
            mockPreview = withTransactions([semCategoria])
            renderPage()
            const user = userEvent.setup()

            await user.click(screen.getByRole('button', { name: /desmarcar sem categoria/i }))

            await waitFor(() => {
                expect(screen.getByRole('button', { name: /confirmar/i })).not.toBeDisabled()
            })
            expect(screen.getByRole('checkbox', { name: 'Selecionar LOJA SEM CATEGORIA' })).not.toBeChecked()
        })

        it('não deve bloquear transferência sem categoria', async () => {
            mockPreview = withTransactions([
                {
                    ...semCategoria,
                    id: 'bt-6',
                    description: 'TRANSFERENCIA PIX JOAO',
                },
            ])
            renderPage()
            const user = userEvent.setup()

            expect(screen.getByRole('button', { name: /confirmar/i })).toBeDisabled()

            const row = screen.getByTestId('import-row-bt-6')
            await user.click(within(row).getByTitle('Marcar como transferência'))

            await waitFor(() => {
                expect(screen.getByRole('button', { name: /confirmar/i })).not.toBeDisabled()
            })
        })
    })

    describe('processamento', () => {
        it('deve aguardar o processamento antes de mostrar a tabela', async () => {
            mockPreview = { ...basePreview, status: 'Processing' }
            renderPage()

            expect(screen.getByText(/verificando duplicatas/i)).toBeInTheDocument()
            expect(screen.queryByRole('button', { name: /confirmar/i })).not.toBeInTheDocument()
            expect(screen.queryByText('IFOOD RESTAURANTE')).not.toBeInTheDocument()
        })

        it('deve avisar quando o processamento falhou', () => {
            mockPreview = { ...basePreview, status: 'Failed' }
            renderPage()

            expect(screen.getByRole('alert')).toHaveTextContent(/não foi possível processar/i)
        })
    })

    describe('conta de destino', () => {
        it('deve exibir a conta atual da importação', () => {
            renderPage()
            const select = screen.getByRole('combobox', { name: 'select.label' }) as HTMLSelectElement
            expect(select.value).toBe('acc-1')
        })

        it('deve trocar a conta e refazer a verificação de duplicatas', async () => {
            renderPage()
            const user = userEvent.setup()

            await user.selectOptions(
                screen.getByRole('combobox', { name: 'select.label' }),
                'acc-2'
            )

            expect(mockChangeAccountMutate).toHaveBeenCalledWith('acc-2', expect.any(Object))
        })
    })
})
