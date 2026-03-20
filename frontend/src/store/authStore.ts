import { create } from 'zustand'
import type { UserProfile } from '@/features/auth/types/auth.types'

interface AuthState {
  user: UserProfile | null
  isAuthenticated: boolean
  setUser: (user: UserProfile, token: string) => void
  updateUser: (user: UserProfile) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>(set => ({
  user: null,
  isAuthenticated: !!sessionStorage.getItem('accessToken'),

  setUser: (user, token) => {
    sessionStorage.setItem('accessToken', token)
    set({ user, isAuthenticated: true })
  },

  updateUser: user => set({ user }),

  logout: () => {
    sessionStorage.removeItem('accessToken')
    set({ user: null, isAuthenticated: false })
  },
}))