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
})