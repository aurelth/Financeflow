import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import DeleteAccountDialog from '@/features/settings/components/DeleteAccountDialog'

const mockMutate = vi.fn()

vi.mock('@/features/settings/api/useSettings', () => ({
    useDeleteAccount: () => ({
        mutate: mockMutate,
        isPending: false,
    }),
}))

const renderDialog = (onClose = vi.fn()) => {
    const qc = new QueryClient()
    return render(
        <QueryClientProvider client={qc}>
            <MemoryRouter>
                <DeleteAccountDialog onClose={onClose} />
            </MemoryRouter>
        </QueryClientProvider>
    )
}

describe('DeleteAccountDialog', () => {
    beforeEach(() => vi.clearAllMocks())

    it('deve renderizar o modal correctamente', () => {
        renderDialog()        
        expect(screen.getByRole('heading', { name: /excluir conta/i })).toBeInTheDocument()
        expect(screen.getByText(/esta ação é permanente/i)).toBeInTheDocument()
        expect(screen.getByLabelText(/confirme a sua senha/i)).toBeInTheDocument()
    })

    it('deve chamar onClose ao clicar em cancelar', async () => {
        const onClose = vi.fn()
        renderDialog(onClose)
        const user = userEvent.setup()

        await user.click(screen.getByRole('button', { name: /cancelar/i }))

        expect(onClose).toHaveBeenCalledOnce()
    })

    it('deve manter botão de confirmar desabilitado quando senha está vazia', () => {
        renderDialog()
        const confirmBtn = screen.getByRole('button', { name: /excluir conta/i })
        expect(confirmBtn).toBeDisabled()
    })

    it('deve habilitar botão de confirmar ao preencher a senha', async () => {
        renderDialog()
        const user = userEvent.setup()

        await user.type(screen.getByLabelText(/confirme a sua senha/i), 'Senha@123')

        const confirmBtn = screen.getByRole('button', { name: /excluir conta/i })
        expect(confirmBtn).not.toBeDisabled()
    })

    it('deve chamar mutate com a senha ao confirmar', async () => {
        renderDialog()
        const user = userEvent.setup()

        await user.type(screen.getByLabelText(/confirme a sua senha/i), 'Senha@123')
        await user.click(screen.getByRole('button', { name: /excluir conta/i }))

        expect(mockMutate).toHaveBeenCalledWith({ currentPassword: 'Senha@123' })
    })

    it('deve alternar visibilidade da senha', async () => {
        renderDialog()
        const user = userEvent.setup()

        const input = screen.getByLabelText(/confirme a sua senha/i)
        expect(input).toHaveAttribute('type', 'password')

        const toggleBtn = input.parentElement?.querySelector('button')
        if (toggleBtn) await user.click(toggleBtn)

        expect(input).toHaveAttribute('type', 'text')
    })
})