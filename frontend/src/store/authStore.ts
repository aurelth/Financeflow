import { create } from 'zustand'
import type { UserProfile } from '@/features/auth/types/auth.types'

interface AuthState {
  user:            UserProfile | null
  isAuthenticated: boolean
  setUser:         (user: UserProfile, token: string) => void
  updateUser:      (user: UserProfile) => void
  logout:          () => void
}

export const useAuthStore = create<AuthState>(set => ({
  // Recupera o usuário do sessionStorage ao inicializar
  user: (() => {
    try {
      const stored = sessionStorage.getItem('user')
      return stored ? JSON.parse(stored) : null
    } catch {
      return null
    }
  })(),
  isAuthenticated: !!sessionStorage.getItem('accessToken'),

  setUser: (user, token) => {
    sessionStorage.setItem('accessToken', token)
    // Persiste o usuário no sessionStorage
    sessionStorage.setItem('user', JSON.stringify(user))
    set({ user, isAuthenticated: true })
  },

  updateUser: user => {
    // Atualiza o usuário no sessionStorage
    sessionStorage.setItem('user', JSON.stringify(user))
    set({ user })
  },

  logout: () => {
    sessionStorage.removeItem('accessToken')
    // Remove o usuário do sessionStorage
    sessionStorage.removeItem('user')
    set({ user: null, isAuthenticated: false })
  },
}))