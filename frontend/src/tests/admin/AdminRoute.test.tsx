import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import AdminRoute from '@/routes/AdminRoute'

vi.mock('@/store/authStore', () => ({
  useAuthStore: (selector: any) => selector({
    user: { role: 'Admin' },
  }),
}))

vi.mock('@/store/authStore', () => ({
  useAuthStore: vi.fn(),
}))

import { useAuthStore } from '@/store/authStore'

const renderWithRole = (role: 'Admin' | 'User' | null) => {
  ;(useAuthStore as any).mockImplementation((selector: any) =>
    selector({ user: role ? { role } : null })
  )

  return render(
    <MemoryRouter initialEntries={['/admin']}>
      <Routes>
        <Route element={<AdminRoute />}>
          <Route path="/admin" element={<div>Painel Admin</div>} />
        </Route>
        <Route path="/dashboard" element={<div>Dashboard</div>} />
      </Routes>
    </MemoryRouter>
  )
}

describe('AdminRoute', () => {
  it('deve renderizar a página quando usuário é Admin', () => {
    renderWithRole('Admin')
    expect(screen.getByText('Painel Admin')).toBeInTheDocument()
  })

  it('deve redirecionar para /dashboard quando usuário é User comum', () => {
    renderWithRole('User')
    expect(screen.getByText('Dashboard')).toBeInTheDocument()
    expect(screen.queryByText('Painel Admin')).not.toBeInTheDocument()
  })

  it('deve redirecionar para /dashboard quando não há usuário', () => {
    renderWithRole(null)
    expect(screen.getByText('Dashboard')).toBeInTheDocument()
    expect(screen.queryByText('Painel Admin')).not.toBeInTheDocument()
  })
})