import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import CategoryForm from '@/features/categories/components/CategoryForm'
import { TransactionType, type Category } from '@/features/categories/types/category.types'

const mockCategory: Category = {
  id:            '1',
  name:          'Alimentação',
  icon:          '🍔',
  color:         '#6366f1',
  type:          TransactionType.Expense,
  isDefault:     false,
  isActive:      true,
  isOwner:       true,
  subcategories: [],
}

const renderForm = (props: Partial<React.ComponentProps<typeof CategoryForm>> = {}) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <CategoryForm
          isPending={false}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
          {...props}
        />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('CategoryForm', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar campos do formulário no modo criação', () => {
    renderForm()
    expect(screen.getByPlaceholderText('Ex: Alimentação')).toBeInTheDocument()
    expect(screen.getByText('Receita')).toBeInTheDocument()
    expect(screen.getByText('Despesa')).toBeInTheDocument()
  })

  it('deve preencher campos com valores da categoria no modo edição', () => {
    renderForm({ category: mockCategory })
    expect(screen.getByPlaceholderText('Ex: Alimentação')).toHaveValue('Alimentação')
  })

  it('não deve exibir seletor de tipo no modo edição', () => {
    renderForm({ category: mockCategory })
    expect(screen.queryByText('Receita')).not.toBeInTheDocument()
    expect(screen.queryByText('Despesa')).not.toBeInTheDocument()
  })

  it('deve mostrar erro de validação com nome muito curto', async () => {
    renderForm()
    const user = userEvent.setup()

    await user.type(screen.getByPlaceholderText('Ex: Alimentação'), 'A')
    await user.click(screen.getByRole('button', { name: /criar categoria/i }))

    await waitFor(() => {
      expect(screen.getByText(/mínimo 2 caracteres/i)).toBeInTheDocument()
    })
  })

  it('deve chamar onCancel ao clicar em cancelar', async () => {
    const onCancel = vi.fn()
    renderForm({ onCancel })
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: /cancelar/i }))
    expect(onCancel).toHaveBeenCalled()
  })

  it('deve exibir texto correto no botão no modo edição', () => {
    renderForm({ category: mockCategory })
    expect(screen.getByRole('button', { name: /salvar alterações/i })).toBeInTheDocument()
  })

  it('deve exibir texto correto no botão no modo criação', () => {
    renderForm()
    expect(screen.getByRole('button', { name: /criar categoria/i })).toBeInTheDocument()
  })

  it('deve exibir loader quando isPending é true', () => {
    renderForm({ isPending: true })
    expect(screen.getByText(/salvando/i)).toBeInTheDocument()
  })
})