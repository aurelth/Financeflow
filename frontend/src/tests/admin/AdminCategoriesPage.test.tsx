import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import AdminCategoriesPage from '@/features/admin/pages/AdminCategoriesPage'

const mockCreate = vi.fn()
const mockUpdate = vi.fn()
const mockDelete = vi.fn()

const mockCategories = [
  {
    id:        '1',
    name:      'Alimentação',
    icon:      'utensils',
    color:     '#f97316',
    type:      'Expense',
    isActive:  true,
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id:        '2',
    name:      'Salário',
    icon:      'briefcase',
    color:     '#22c55e',
    type:      'Income',
    isActive:  true,
    createdAt: '2026-01-01T00:00:00Z',
  },
]

vi.mock('@/features/admin/api/useAdmin', () => ({
  useAdminCategories:        () => ({ data: mockCategories, isLoading: false }),
  useCreateDefaultCategory:  () => ({ mutate: mockCreate, isPending: false }),
  useUpdateDefaultCategory:  () => ({ mutate: mockUpdate, isPending: false }),
  useDeleteDefaultCategory:  () => ({ mutate: mockDelete, isPending: false }),
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <AdminCategoriesPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('AdminCategoriesPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByText('Categorias Padrão')).toBeInTheDocument()
  })

  it('deve exibir a lista de categorias', () => {
    renderPage()
    expect(screen.getByText('Alimentação')).toBeInTheDocument()
    expect(screen.getByText('Salário')).toBeInTheDocument()
  })

  it('deve abrir modal de criação ao clicar em nova categoria', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /nova categoria/i }))

    await waitFor(() => {
      expect(screen.getByText('Nova categoria padrão')).toBeInTheDocument()
    })
  })

  it('deve abrir modal de edição ao clicar em editar', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getAllByTitle('Editar')[0])

    await waitFor(() => {
      expect(screen.getByText('Editar categoria padrão')).toBeInTheDocument()
    })
  })

  it('deve abrir modal de exclusão ao clicar em excluir', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getAllByTitle('Excluir')[0])

    await waitFor(() => {
      expect(screen.getByText('Excluir categoria padrão')).toBeInTheDocument()
    })
  })

  it('deve fechar modal ao clicar em cancelar', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /nova categoria/i }))
    await user.click(screen.getByRole('button', { name: /cancelar/i }))

    await waitFor(() => {
      expect(screen.queryByText('Nova categoria padrão')).not.toBeInTheDocument()
    })
  })

  it('deve filtrar apenas despesas ao clicar em Despesas', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /despesas/i }))

    await waitFor(() => {
      expect(screen.getByText('Alimentação')).toBeInTheDocument()
      expect(screen.queryByText('Salário')).not.toBeInTheDocument()
    })
  })

  it('deve filtrar apenas receitas ao clicar em Receitas', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /receitas/i }))

    await waitFor(() => {
      expect(screen.getByText('Salário')).toBeInTheDocument()
      expect(screen.queryByText('Alimentação')).not.toBeInTheDocument()
    })
  })
})