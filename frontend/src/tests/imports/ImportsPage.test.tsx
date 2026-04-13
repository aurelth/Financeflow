import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ImportsPage from '@/features/imports/pages/ImportsPage'
import type { BankImportDto } from '@/features/imports/types/imports.types'

const mockNavigate = vi.fn()
const mockUploadMutate = vi.fn()
const mockRefetch = vi.fn()

vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual('react-router-dom')
    return { ...actual, useNavigate: () => mockNavigate }
})

// Lista mutável para controlar dados por teste
let mockImportsList: BankImportDto[] = []

vi.mock('@/features/imports/api/useImports', () => ({
    useImports: () => ({ data: mockImportsList, isLoading: false, refetch: mockRefetch }),
    useUploadOFX: () => ({ mutate: mockUploadMutate, isPending: false }),
}))

const mockImports: BankImportDto[] = [
    {
        id: 'imp-1',
        fileName: 'extrato-janeiro.ofx',
        status: 'Completed',
        totalRecords: 5,
        imported: 4,
        duplicates: 1,
        errors: 0,
        errorMessage: null,
        createdAt: '2026-01-15T10:00:00',
    },
    {
        id: 'imp-2',
        fileName: 'extrato-fevereiro.ofx',
        status: 'Pending',
        totalRecords: 3,
        imported: 0,
        duplicates: 0,
        errors: 0,
        errorMessage: null,
        createdAt: '2026-02-10T08:00:00',
    },
]

const renderPage = () => {
    const qc = new QueryClient()
    return render(
        <QueryClientProvider client={qc}>
            <MemoryRouter>
                <ImportsPage />
            </MemoryRouter>
        </QueryClientProvider>
    )
}

describe('ImportsPage', () => {
    beforeEach(() => {
        vi.clearAllMocks()
        mockImportsList = [...mockImports] // Reset da lista antes de cada teste
    })

    it('deve renderizar o título da página', () => {
        renderPage()
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Importar OFX')
    })

    it('deve exibir a zona de upload', () => {
        renderPage()
        expect(screen.getByText('Arraste o ficheiro OFX aqui')).toBeInTheDocument()
    })

    it('deve exibir o histórico de importações', () => {
        renderPage()
        expect(screen.getByText('extrato-janeiro.ofx')).toBeInTheDocument()
        expect(screen.getByText('extrato-fevereiro.ofx')).toBeInTheDocument()
    })

    it('deve exibir o status correto para cada importação', () => {
        renderPage()
        expect(screen.getByText('Concluído')).toBeInTheDocument()
        expect(screen.getByText('Pendente')).toBeInTheDocument()
    })

    it('deve exibir o total de transações de cada importação', () => {
        renderPage()
        expect(screen.getByText(/5 transações/)).toBeInTheDocument()
        expect(screen.getByText(/3 transações/)).toBeInTheDocument()
    })

    it('deve exibir botão "Ver preview" para importações Completed e Pending', () => {
        renderPage()
        const previewButtons = screen.getAllByRole('button', { name: /ver preview/i })
        expect(previewButtons).toHaveLength(2)
    })

    it('deve navegar para preview ao clicar em "Ver preview"', async () => {
        renderPage()
        const user = userEvent.setup()

        const previewButtons = screen.getAllByRole('button', { name: /ver preview/i })
        await user.click(previewButtons[0])

        expect(mockNavigate).toHaveBeenCalledWith('/imports/imp-1/preview')
    })

    it('deve exibir resumo de importadas e duplicadas para importações concluídas', () => {
        renderPage()
        expect(screen.getByText(/4 importadas/)).toBeInTheDocument()
        expect(screen.getByText(/1 duplicadas/)).toBeInTheDocument()
    })

    it('deve exibir estado vazio quando não há importações', () => {
        mockImportsList = []
        renderPage()
        expect(screen.getByText('Nenhuma importação encontrada')).toBeInTheDocument()
    })

    it('deve exibir erro ao selecionar ficheiro não OFX', async () => {
        renderPage()

        const input = document.getElementById('ofx-input') as HTMLInputElement
        const file = new File(['csv,data'], 'extrato.csv', { type: 'text/csv' })

        // Dispara evento diretamente pois o input está hidden
        Object.defineProperty(input, 'files', { value: [file], configurable: true })
        input.dispatchEvent(new Event('change', { bubbles: true }))

        await waitFor(() => {
            expect(screen.getByText('Apenas ficheiros .ofx são suportados.')).toBeInTheDocument()
        })
    })

    it('deve exibir ficheiro selecionado ao escolher OFX válido', async () => {
        renderPage()
        const user = userEvent.setup()

        const input = document.getElementById('ofx-input') as HTMLInputElement
        const file = new File(['ofx content'], 'extrato.ofx', { type: 'application/octet-stream' })

        await user.upload(input, file)

        await waitFor(() => {
            expect(screen.getByText('extrato.ofx')).toBeInTheDocument()
            expect(screen.getByRole('button', { name: /enviar/i })).toBeInTheDocument()
        })
    })

    it('deve chamar upload ao clicar em enviar', async () => {
        renderPage()
        const user = userEvent.setup()

        const input = document.getElementById('ofx-input') as HTMLInputElement
        const file = new File(['ofx content'], 'extrato.ofx', { type: 'application/octet-stream' })

        await user.upload(input, file)

        await waitFor(() => {
            expect(screen.getByRole('button', { name: /enviar/i })).toBeInTheDocument()
        })

        await user.click(screen.getByRole('button', { name: /enviar/i }))

        expect(mockUploadMutate).toHaveBeenCalledWith(file, expect.any(Object))
    })

    it('deve limpar ficheiro selecionado ao clicar em X', async () => {
        renderPage()
        const user = userEvent.setup()

        const input = document.getElementById('ofx-input') as HTMLInputElement
        const file = new File(['ofx content'], 'extrato.ofx', { type: 'application/octet-stream' })

        await user.upload(input, file)

        await waitFor(() => {
            expect(screen.getByText('extrato.ofx')).toBeInTheDocument()
        })

        const clearButton = screen.getAllByRole('button').find(btn =>
            btn.querySelector('svg') && !btn.textContent?.includes('Enviar')
            && !btn.textContent?.includes('Atualizar')
        )
        await user.click(clearButton!)

        await waitFor(() => {
            expect(screen.queryByRole('button', { name: /enviar/i })).not.toBeInTheDocument()
        })
    })
})