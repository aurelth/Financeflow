import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import AdminDashboardPage from '@/features/admin/pages/AdminDashboardPage'

vi.mock('@/features/admin/api/useAdmin', () => ({
  useAdminMetrics: () => ({
    data: {
      totalUsers:        10,
      activeUsers:        8,
      inactiveUsers:      2,
      totalAdmins:        1,
      totalCategories:   13,
      defaultCategories: 13,
    },
    isLoading: false,
  }),
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <AdminDashboardPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('AdminDashboardPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByText('Painel Administrativo')).toBeInTheDocument()
  })

  it('deve exibir os cards de métricas', () => {
    renderPage()
    expect(screen.getByText('Total de usuários')).toBeInTheDocument()
    expect(screen.getByText('Usuários ativos')).toBeInTheDocument()
    expect(screen.getByText('Usuários inativos')).toBeInTheDocument()
    expect(screen.getByText('Admins ativos')).toBeInTheDocument()
    expect(screen.getAllByText('Categorias padrão').length).toBeGreaterThanOrEqual(1)
  })

  it('deve exibir os valores corretos nas métricas', () => {
    renderPage()
    expect(screen.getByText('10')).toBeInTheDocument()
    expect(screen.getByText('8')).toBeInTheDocument()
    expect(screen.getByText('2')).toBeInTheDocument()
    expect(screen.getByText('1')).toBeInTheDocument()
    expect(screen.getAllByText(/13/).length).toBeGreaterThanOrEqual(1)
  })

  it('deve exibir as ações rápidas', () => {
    renderPage()
    expect(screen.getByText('Gerenciar usuários')).toBeInTheDocument()
    expect(screen.getAllByText('Categorias padrão').length).toBeGreaterThanOrEqual(2)
  })
})