import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import AdminUsersPage from '@/features/admin/pages/AdminUsersPage'

const mockDeactivate = vi.fn()
const mockReactivate = vi.fn()
const mockPromote = vi.fn()
const mockDemote = vi.fn()

const mockUsers = [
    {
        id: '1',
        name: 'Aurel Admin',
        email: 'admin@teste.com',
        cpf: '000.000.000-00',
        gender: 'Male',
        role: 'Admin',
        currency: 'BRL',
        timezone: 'America/Sao_Paulo',
        isActive: true,
        createdAt: '2026-01-01T00:00:00Z',
        deletedAt: null,
    },
    {
        id: '2',
        name: 'Usuário Comum',
        email: 'user@teste.com',
        cpf: '111.111.111-11',
        gender: 'Female',
        role: 'User',
        currency: 'BRL',
        timezone: 'America/Sao_Paulo',
        isActive: true,
        createdAt: '2026-01-02T00:00:00Z',
        deletedAt: null,
    },
    {
        id: '3',
        name: 'Usuário Inativo',
        email: 'inativo@teste.com',
        cpf: '222.222.222-22',
        gender: 'Male',
        role: 'User',
        currency: 'BRL',
        timezone: 'America/Sao_Paulo',
        isActive: false,
        createdAt: '2026-01-03T00:00:00Z',
        deletedAt: '2026-02-01T00:00:00Z',
    },
]

vi.mock('@/features/admin/api/useAdmin', () => ({
    useAdminUsers: () => ({
        data: {
            users: mockUsers,
            total: 3,
            page: 1,
            pageSize: 20,
            totalPages: 1,
        },
        isLoading: false,
    }),
    useDeactivateUser: () => ({ mutate: mockDeactivate, isPending: false }),
    useReactivateUser: () => ({ mutate: mockReactivate, isPending: false }),
    usePromoteUser: () => ({ mutate: mockPromote, isPending: false }),
    useDemoteUser: () => ({ mutate: mockDemote, isPending: false }),
}))

const renderPage = () => {
    const qc = new QueryClient()
    return render(
        <QueryClientProvider client={qc}>
            <MemoryRouter>
                <AdminUsersPage />
            </MemoryRouter>
        </QueryClientProvider>
    )
}

describe('AdminUsersPage', () => {
    beforeEach(() => vi.clearAllMocks())

    it('deve renderizar o título da página', () => {
        renderPage()
        expect(screen.getByText('Gestão de Usuários')).toBeInTheDocument()
    })

    it('deve exibir a lista de usuários', () => {
        renderPage()
        expect(screen.getByText('Aurel Admin')).toBeInTheDocument()
        expect(screen.getByText('Usuário Comum')).toBeInTheDocument()
        expect(screen.getByText('Usuário Inativo')).toBeInTheDocument()
    })

    it('deve exibir badges de status corretos', () => {
        renderPage()
        expect(screen.getByText('Admin')).toBeInTheDocument()
        expect(screen.getAllByText('Ativo').length).toBeGreaterThan(0)
        expect(screen.getByText('Inativo')).toBeInTheDocument()
    })

    it('deve abrir modal de confirmação ao clicar em desativar', async () => {
        renderPage()
        const user = userEvent.setup()

        const desativarBtns = screen.getAllByTitle('Desativar')
        await user.click(desativarBtns[0])

        await waitFor(() => {            
            expect(screen.getByRole('heading', { name: /desativar usuário/i })).toBeInTheDocument()
        })
    })

    it('deve abrir modal de confirmação ao clicar em promover', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getByTitle('Promover a Admin'))

        await waitFor(() => {
            expect(screen.getByRole('heading', { name: /promover a admin/i })).toBeInTheDocument()
        })
    })

    it('deve fechar modal ao clicar em cancelar', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getAllByTitle('Desativar')[0])
        await user.click(screen.getByRole('button', { name: /cancelar/i }))

        await waitFor(() => {
            expect(screen.queryByText('Deseja desativar')).not.toBeInTheDocument()
        })
    })

    it('deve chamar deactivate ao confirmar desativação', async () => {
        renderPage()
        const user = userEvent.setup()

        await user.click(screen.getAllByTitle('Desativar')[0])

        // Modificado: pega o último botão com esse texto (é o de confirmação)
        await waitFor(async () => {
            const btns = screen.getAllByRole('button', { name: /desativar usuário/i })
            await user.click(btns[btns.length - 1])
        })

        expect(mockDeactivate).toHaveBeenCalledWith('1', expect.any(Object))
    })

    it('deve exibir botão de reativar para usuário inativo', () => {
        renderPage()
        expect(screen.getByTitle('Reativar')).toBeInTheDocument()
    })
})