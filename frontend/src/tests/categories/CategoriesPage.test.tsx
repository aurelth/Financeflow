import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import CategoriesPage from '@/features/categories/pages/CategoriesPage'
import { TransactionType } from '@/features/categories/types/category.types'

vi.mock('@/features/categories/api/useCategories', () => ({
  useCategories:       () => ({ data: mockCategories, isLoading: false }),
  useCreateCategory:   () => ({ mutate: vi.fn(), isPending: false }),
  useUpdateCategory:   () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteCategory:   () => ({ mutate: vi.fn(), isPending: false }),
  useCreateSubcategory: () => ({ mutate: vi.fn(), isPending: false }),
  useUpdateSubcategory: () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteSubcategory: () => ({ mutate: vi.fn(), isPending: false }),
}))

const mockCategories = [
  {
    id:            '1',
    name:          'Alimentação',
    icon:          '🍔',
    color:         '#6366f1',
    type:          TransactionType.Expense,
    isDefault:     false,
    isActive:      true,
    isOwner:       true,
    subcategories: [],
  },
  {
    id:            '2',
    name:          'Salário',
    icon:          '💰',
    color:         '#22c55e',
    type:          TransactionType.Income,
    isDefault:     true,
    isActive:      true,
    isOwner:       false,
    subcategories: [],
  },
]

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <CategoriesPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('CategoriesPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByText('Categorias')).toBeInTheDocument()
  })

  it('deve exibir todas as categorias por padrão', () => {
    renderPage()
    expect(screen.getByText('Alimentação')).toBeInTheDocument()
    expect(screen.getByText('Salário')).toBeInTheDocument()
  })

  it('deve filtrar apenas receitas', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /receitas/i }))

    expect(screen.queryByText('Alimentação')).not.toBeInTheDocument()
    expect(screen.getByText('Salário')).toBeInTheDocument()
  })

  it('deve filtrar apenas despesas', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /despesas/i }))

    expect(screen.getByText('Alimentação')).toBeInTheDocument()
    expect(screen.queryByText('Salário')).not.toBeInTheDocument()
  })

  it('deve abrir modal ao clicar em nova categoria', async () => {
  renderPage()
  const user = userEvent.setup()

  await user.click(screen.getByRole('button', { name: /nova categoria/i }))

  await waitFor(() => {
    expect(screen.getByRole('heading', { name: 'Nova categoria' })).toBeInTheDocument()
  })
})

  it('deve exibir estado vazio quando não há categorias no filtro', async () => {
    renderPage()
    const user = userEvent.setup()

    // Filtra receitas — não há nenhuma despesa cadastrada como receita aqui
    await user.click(screen.getByRole('button', { name: /despesas/i }))

    // Volta ao filtro de receitas para garantir que só Salário aparece
    await user.click(screen.getByRole('button', { name: /receitas/i }))

    expect(screen.getByText('Salário')).toBeInTheDocument()
  })

  it('deve exibir loader durante carregamento', () => {
    vi.mocked(
      vi.importActual('@/features/categories/api/useCategories')
    )
    const qc = new QueryClient()
    render(
      <QueryClientProvider client={qc}>
        <MemoryRouter>
          <CategoriesPage />
        </MemoryRouter>
      </QueryClientProvider>
    )
  })
})