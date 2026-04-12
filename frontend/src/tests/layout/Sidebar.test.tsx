import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import Sidebar from '@/components/layout/Sidebar'

vi.mock('@/store/authStore', () => ({
  useAuthStore: vi.fn(),
}))

import { useAuthStore } from '@/store/authStore'

const renderSidebar = (role: 'Admin' | 'User') => {
  ;(useAuthStore as any).mockImplementation((selector: any) =>
    selector({ user: { role } })
  )

  return render(
    <MemoryRouter>
      <Sidebar />
    </MemoryRouter>
  )
}

describe('Sidebar', () => {
  it('deve exibir o link Painel Admin para usuário Admin', () => {
    renderSidebar('Admin')
    expect(screen.getByText('Painel Admin')).toBeInTheDocument()
  })

  it('não deve exibir o link Painel Admin para usuário comum', () => {
    renderSidebar('User')
    expect(screen.queryByText('Painel Admin')).not.toBeInTheDocument()
  })

  it('deve sempre exibir os links de navegação principais', () => {
    renderSidebar('User')
    expect(screen.getByText('Dashboard')).toBeInTheDocument()
    expect(screen.getByText('Transações')).toBeInTheDocument()
    expect(screen.getByText('Categorias')).toBeInTheDocument()
    expect(screen.getByText('Configurações')).toBeInTheDocument()
  })
})