import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import CategoryCard from '@/features/categories/components/CategoryCard'
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
  subcategories: [
    { id: 'sub1', name: 'Restaurante', isActive: true },
    { id: 'sub2', name: 'Mercado',     isActive: true },
  ],
}

const mockDefaultCategory: Category = {
  ...mockCategory,
  id:        '2',
  name:      'Salário',
  type:      TransactionType.Income,
  isDefault: true,
  isOwner:   false,
  subcategories: [],
}

const defaultProps = {
  onEdit:      vi.fn(),
  onDelete:    vi.fn(),
  onAddSub:    vi.fn(),
  onEditSub:   vi.fn(),
  onDeleteSub: vi.fn(),
}

const renderCard = (category = mockCategory) =>
  render(<CategoryCard category={category} {...defaultProps} />)

describe('CategoryCard', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar nome e ícone da categoria', () => {
    renderCard()
    expect(screen.getByText('Alimentação')).toBeInTheDocument()
    expect(screen.getByText('🍔')).toBeInTheDocument()
  })

  it('deve exibir o tipo correto para despesa', () => {
    renderCard()
    expect(screen.getByText('Despesa')).toBeInTheDocument()
  })

  it('deve exibir o tipo correto para receita', () => {
    renderCard(mockDefaultCategory)
    expect(screen.getByText('Receita')).toBeInTheDocument()
  })

  it('deve exibir badge "Padrão" para categorias padrão', () => {
    renderCard(mockDefaultCategory)
    expect(screen.getByText('Padrão')).toBeInTheDocument()
  })

  it('não deve exibir botões de ação para categorias padrão', () => {
    renderCard(mockDefaultCategory)
    expect(screen.queryByTitle('Editar')).not.toBeInTheDocument()
    expect(screen.queryByTitle('Remover')).not.toBeInTheDocument()
  })

  it('deve exibir botões de ação para categorias do utilizador', () => {
    renderCard()
    expect(screen.getByTitle('Editar')).toBeInTheDocument()
    expect(screen.getByTitle('Remover')).toBeInTheDocument()
    expect(screen.getByTitle('Adicionar subcategoria')).toBeInTheDocument()
  })

  it('deve chamar onEdit ao clicar em editar', async () => {
    renderCard()
    const user = userEvent.setup()
    await user.click(screen.getByTitle('Editar'))
    expect(defaultProps.onEdit).toHaveBeenCalledWith(mockCategory)
  })

  it('deve chamar onDelete ao clicar em remover', async () => {
    renderCard()
    const user = userEvent.setup()
    await user.click(screen.getByTitle('Remover'))
    expect(defaultProps.onDelete).toHaveBeenCalledWith(mockCategory)
  })

  it('deve chamar onAddSub ao clicar em adicionar subcategoria', async () => {
    renderCard()
    const user = userEvent.setup()
    await user.click(screen.getByTitle('Adicionar subcategoria'))
    expect(defaultProps.onAddSub).toHaveBeenCalledWith(mockCategory)
  })

  it('deve expandir subcategorias ao clicar no chevron', async () => {
    renderCard()
    const user = userEvent.setup()

    expect(screen.queryByText('Restaurante')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: '' }))

    expect(screen.getByText('Restaurante')).toBeInTheDocument()
    expect(screen.getByText('Mercado')).toBeInTheDocument()
  })

  it('deve renderizar ícone Lucide para categorias padrão', () => {
  const categoryWithLucideIcon: Category = {
    ...mockCategory,
    icon:  'briefcase',
    color: '#22c55e',
  }
  render(<CategoryCard category={categoryWithLucideIcon} {...defaultProps} />)
  // O ícone Lucide deve ser renderizado sem exibir o nome do ícone como texto
  expect(screen.queryByText('briefcase')).not.toBeInTheDocument()
  })

  it('deve renderizar emoji para categorias do utilizador', () => {
    renderCard()
    expect(screen.getByText('🍔')).toBeInTheDocument()
    expect(screen.queryByText('utensils')).not.toBeInTheDocument()
  })
})