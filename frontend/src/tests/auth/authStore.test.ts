import { describe, it, expect, beforeEach } from 'vitest'
import { useAuthStore } from '@/store/authStore'
import type { UserProfile } from '@/features/auth/types/auth.types'

const mockUser: UserProfile = {
  id:        '123e4567-e89b-12d3-a456-426614174000',
  name:      'Aurel Teste',
  email:     'aurel@teste.com',
  cpf:       '529.982.247-25',
  gender:    'Male',
  currency:  'BRL',
  timezone:  'America/Sao_Paulo',
  createdAt: '2026-01-01T00:00:00Z',
  role:      'User',
}

const mockAdmin: UserProfile = {
  ...mockUser,
  id:   '999e4567-e89b-12d3-a456-426614174000',
  role: 'Admin',
}

describe('authStore', () => {
  beforeEach(() => {
    sessionStorage.clear()
    useAuthStore.setState({ user: null, isAuthenticated: false })
  })

  it('deve iniciar com utilizador nulo e não autenticado', () => {
    const { user, isAuthenticated } = useAuthStore.getState()
    expect(user).toBeNull()
    expect(isAuthenticated).toBe(false)
  })

  it('deve autenticar o utilizador ao chamar setUser', () => {
    useAuthStore.getState().setUser(mockUser, 'access_token_123')

    const { user, isAuthenticated } = useAuthStore.getState()
    expect(user).toEqual(mockUser)
    expect(isAuthenticated).toBe(true)
    expect(sessionStorage.getItem('accessToken')).toBe('access_token_123')
  })

  it('deve atualizar o perfil ao chamar updateUser', () => {
    useAuthStore.getState().setUser(mockUser, 'token')

    const updatedUser = { ...mockUser, name: 'Aurel Atualizado' }
    useAuthStore.getState().updateUser(updatedUser)

    const { user } = useAuthStore.getState()
    expect(user?.name).toBe('Aurel Atualizado')
  })

  it('deve desautenticar ao chamar logout', () => {
    useAuthStore.getState().setUser(mockUser, 'token')
    useAuthStore.getState().logout()

    const { user, isAuthenticated } = useAuthStore.getState()
    expect(user).toBeNull()
    expect(isAuthenticated).toBe(false)
    expect(sessionStorage.getItem('accessToken')).toBeNull()
  })

  // Persistência do usuário no sessionStorage
  it('deve persistir o usuário no sessionStorage ao chamar setUser', () => {
    useAuthStore.getState().setUser(mockUser, 'token')

    const stored = sessionStorage.getItem('user')
    expect(stored).not.toBeNull()
    expect(JSON.parse(stored!)).toEqual(mockUser)
  })

  it('deve atualizar o usuário no sessionStorage ao chamar updateUser', () => {
    useAuthStore.getState().setUser(mockUser, 'token')

    const updatedUser = { ...mockUser, name: 'Aurel Atualizado' }
    useAuthStore.getState().updateUser(updatedUser)

    const stored = sessionStorage.getItem('user')
    expect(JSON.parse(stored!).name).toBe('Aurel Atualizado')
  })

  it('deve remover o usuário do sessionStorage ao chamar logout', () => {
    useAuthStore.getState().setUser(mockUser, 'token')
    useAuthStore.getState().logout()

    expect(sessionStorage.getItem('user')).toBeNull()
  })

  // Teste da role no sessionStorage
  it('deve persistir a role Admin no sessionStorage', () => {
    useAuthStore.getState().setUser(mockAdmin, 'admin-token')

    const stored = JSON.parse(sessionStorage.getItem('user')!)
    expect(stored.role).toBe('Admin')
  })
})