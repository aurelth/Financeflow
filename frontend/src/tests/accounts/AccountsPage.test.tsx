import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import AccountsPage from '@/features/accounts/pages/AccountsPage'
import type { Account } from '@/features/accounts/types/account.types'
import { makeAccount, nubank, itau } from './fixtures'

vi.mock('react-i18next', async () => (await import('./i18nMock')).accountsI18nMock)

const mockArchive = vi.fn()
const mockRestore = vi.fn()
const mockCreate  = vi.fn()
const mockUpdate  = vi.fn()
let mockAccounts: Account[] = []
let lastIncludeArchived: boolean | undefined

vi.mock('@/features/accounts/api/useAccounts', () => ({
  useAccounts:       (includeArchived?: boolean) => {
    lastIncludeArchived = includeArchived
    return { data: mockAccounts, isLoading: false }
  },
  useCreateAccount:  () => ({ mutate: mockCreate,  isPending: false }),
  useUpdateAccount:  () => ({ mutate: mockUpdate,  isPending: false }),
  useArchiveAccount: () => ({ mutate: mockArchive, isPending: false }),
  useRestoreAccount: () => ({ mutate: mockRestore, isPending: false }),
}))

const renderPage = () =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <AccountsPage />
    </QueryClientProvider>,
  )

describe('AccountsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockAccounts = [makeAccount(), nubank, itau]
  })

  it('deve exibir o título e todas as contas', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Contas')
    expect(screen.getByText('Conta principal')).toBeInTheDocument()
    expect(screen.getByText('Nubank', { selector: 'p.text-sm' })).toBeInTheDocument()
    expect(screen.getByText('Itaú')).toBeInTheDocument()
  })

  it('deve somar o saldo das contas ativas e indicar quantas são', () => {
    renderPage()
    // 1000 + 2500,50 − 300 = 3200,50
    expect(screen.getByText(/3\.200,50/)).toBeInTheDocument()
    expect(screen.getByText('3 contas ativas')).toBeInTheDocument()
  })

  it('deve marcar a conta principal e mostrar o saldo negativo na cor de despesa', () => {
    renderPage()
    expect(screen.getByText('Principal')).toBeInTheDocument()
    const negative = screen.getByText(/-R\$ 300,00|-\s?R\$\s?300,00/)
    expect(negative).toHaveStyle({ color: 'var(--ff-expense)' })
  })

  it('deve indicar se a conta já é reconhecida nos extratos OFX', () => {
    renderPage()
    expect(screen.getAllByText('Reconhecida nos extratos OFX')).toHaveLength(1)
    expect(screen.getAllByText('Será reconhecida no primeiro extrato OFX importado')).toHaveLength(2)
  })

  it('não deve permitir arquivar a conta principal', () => {
    renderPage()
    expect(screen.queryByLabelText('Arquivar Conta principal')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Arquivar Nubank')).toBeInTheDocument()
  })

  it('deve pedir confirmação antes de arquivar e arquivar a conta confirmada', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByLabelText('Arquivar Nubank'))
    expect(screen.getByRole('alertdialog')).toHaveTextContent('"Nubank"')
    expect(mockArchive).not.toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Arquivar' }))
    expect(mockArchive).toHaveBeenCalledWith('acc-2', expect.any(Object))
  })

  it('deve abrir o formulário de nova conta', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /Nova conta/ }))
    expect(screen.getByRole('dialog', { name: 'Nova conta' })).toBeInTheDocument()
  })

  it('deve abrir o formulário preenchido ao editar', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByLabelText('Editar Nubank'))
    expect(screen.getByRole('dialog', { name: 'Editar conta' })).toBeInTheDocument()
    expect(screen.getByLabelText('Nome')).toHaveValue('Nubank')
  })

  it('deve pedir contas arquivadas só quando o filtro está ligado', async () => {
    renderPage()
    const user = userEvent.setup()
    expect(lastIncludeArchived).toBe(false)

    await user.click(screen.getByLabelText('Mostrar arquivadas'))
    expect(lastIncludeArchived).toBe(true)
  })

  it('deve oferecer reativar para conta arquivada e não somá-la ao saldo total', async () => {
    mockAccounts = [makeAccount(), makeAccount({ id: 'acc-9', name: 'Antiga', isDefault: false, isArchived: true, balance: 9999 })]
    renderPage()
    const user = userEvent.setup()

    expect(screen.getByText('1 conta ativa')).toBeInTheDocument()
    expect(screen.queryByLabelText('Editar Antiga')).not.toBeInTheDocument()

    await user.click(screen.getByLabelText('Reativar Antiga'))
    expect(mockRestore).toHaveBeenCalledWith(expect.objectContaining({ id: 'acc-9' }), expect.any(Object))
  })

  it('deve mostrar o estado vazio quando não há contas', () => {
    mockAccounts = []
    renderPage()
    expect(screen.getByText('Nenhuma conta encontrada')).toBeInTheDocument()
  })
})
