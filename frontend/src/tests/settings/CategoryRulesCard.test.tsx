import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import CategoryRulesCard from '@/features/settings/components/CategoryRulesCard'
import type { CategoryRule } from '@/features/settings/types/settings.types'

const mockDelete = vi.fn()

let mockState: { data?: CategoryRule[]; isLoading: boolean; isError: boolean }
let mockDeleteState: { isPending: boolean; variables?: string }

vi.mock('@/features/settings/api/useCategoryRules', () => ({
  useCategoryRules: () => mockState,
  useDeleteCategoryRule: () => ({
    mutate:    mockDelete,
    isPending: mockDeleteState.isPending,
    variables: mockDeleteState.variables,
  }),
}))

function makeRule(n: number, overrides: Partial<CategoryRule> = {}): CategoryRule {
  return {
    id:            `rule-${n}`,
    key:           `loja ${n}`,
    type:          'Expense',
    categoryId:    'cat-1',
    categoryName:  'Compras',
    categoryIcon:  '🛒',
    categoryColor: '#f59e0b',
    hitCount:      n,
    lastUsedAt:    '2026-02-10T00:00:00Z',
    ...overrides,
  }
}

describe('CategoryRulesCard', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockState       = { data: [], isLoading: false, isError: false }
    mockDeleteState = { isPending: false }
  })

  it('deve mostrar o estado vazio quando nada foi aprendido', () => {
    render(<CategoryRulesCard />)

    expect(screen.getByText('categoryRules.empty')).toBeInTheDocument()
    expect(screen.queryByRole('list')).not.toBeInTheDocument()
  })

  it('deve mostrar a falha ao carregar sem exibir o estado vazio', () => {
    mockState = { data: undefined, isLoading: false, isError: true }
    render(<CategoryRulesCard />)

    expect(screen.getByRole('alert')).toHaveTextContent('categoryRules.error')
    expect(screen.queryByText('categoryRules.empty')).not.toBeInTheDocument()
  })

  it('não deve mostrar o estado vazio enquanto carrega', () => {
    mockState = { data: undefined, isLoading: true, isError: false }
    render(<CategoryRulesCard />)

    expect(screen.queryByText('categoryRules.empty')).not.toBeInTheDocument()
  })

  it('deve listar a descrição aprendida e a categoria', () => {
    mockState = { data: [makeRule(3, { key: 'mercado extra', categoryName: 'Mercado' })], isLoading: false, isError: false }
    render(<CategoryRulesCard />)

    expect(screen.getByText('mercado extra')).toBeInTheDocument()
    expect(screen.getByText(/Mercado/)).toBeInTheDocument()
  })

  it('deve esquecer a regra ao clicar no botão da linha', async () => {
    mockState = { data: [makeRule(1), makeRule(2)], isLoading: false, isError: false }
    render(<CategoryRulesCard />)
    const user = userEvent.setup()

    await user.click(screen.getAllByRole('button', { name: 'categoryRules.forget' })[1])

    expect(mockDelete).toHaveBeenCalledWith('rule-2')
  })

  it('deve desabilitar só o botão da regra que está sendo esquecida', () => {
    mockState       = { data: [makeRule(1), makeRule(2)], isLoading: false, isError: false }
    mockDeleteState = { isPending: true, variables: 'rule-1' }
    render(<CategoryRulesCard />)

    const [first, second] = screen.getAllByRole('button', { name: 'categoryRules.forget' })
    expect(first).toBeDisabled()
    expect(second).toBeEnabled()
  })

  it('deve recolher listas longas e expandir sob demanda', async () => {
    mockState = { data: Array.from({ length: 12 }, (_, i) => makeRule(i + 1)), isLoading: false, isError: false }
    render(<CategoryRulesCard />)
    const user = userEvent.setup()

    expect(screen.getAllByRole('listitem')).toHaveLength(8)

    await user.click(screen.getByRole('button', { name: 'categoryRules.showAll' }))
    expect(screen.getAllByRole('listitem')).toHaveLength(12)

    await user.click(screen.getByRole('button', { name: 'categoryRules.showLess' }))
    expect(screen.getAllByRole('listitem')).toHaveLength(8)
  })

  it('não deve oferecer "ver todas" quando a lista cabe no limite', () => {
    mockState = { data: Array.from({ length: 8 }, (_, i) => makeRule(i + 1)), isLoading: false, isError: false }
    render(<CategoryRulesCard />)

    expect(screen.queryByRole('button', { name: 'categoryRules.showAll' })).not.toBeInTheDocument()
  })
})
