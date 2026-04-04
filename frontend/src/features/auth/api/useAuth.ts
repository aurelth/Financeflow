import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import api from '@/lib/axios'
import { useAuthStore } from '@/store/authStore'
import type {
  AuthResponse,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  UpdateProfileRequest,
  UserProfile,
} from '../types/auth.types'

// Register
export const useRegister = () => {
  const navigate = useNavigate()

  return useMutation({
    mutationFn: (data: RegisterRequest) =>
      api.post<UserProfile>('/api/auth/register', data).then(r => r.data),
    onSuccess: () => {
      toast.success('Conta criada com sucesso! Faça login para continuar.')
      navigate('/login')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error('Erro ao criar conta. Tente novamente.')
      }
    },
  })
}

// Login
export const useLogin = () => {
  const { setUser } = useAuthStore()
  const navigate    = useNavigate()

  return useMutation({
    mutationFn: (data: LoginRequest) =>
      api.post<AuthResponse>('/api/auth/login', data).then(r => r.data),
    onSuccess: data => {
      setUser(data.user, data.accessToken)
      toast.success(`Bem-vindo, ${data.user.name}!`)
      navigate('/dashboard')
    },
    onError: () => {
      toast.error('Email ou senha incorreto.')
    },
  })
}

// Logout
export const useLogout = () => {
  const { logout } = useAuthStore()
  const navigate   = useNavigate()
  const qc         = useQueryClient()

  return useMutation({
    mutationFn: () => api.post('/api/auth/logout'),
    onSettled: () => {
      logout()
      qc.clear()
      navigate('/login')
    },
  })
}

// Get Profile
export const useUserProfile = () => {
  const { isAuthenticated } = useAuthStore()

  return useQuery({
    queryKey:  ['user', 'profile'],
    queryFn:   () => api.get<UserProfile>('/api/users/profile').then(r => r.data),
    enabled:   isAuthenticated,
    staleTime: 5 * 60 * 1000,
  })
}

// Update Profile
export const useUpdateProfile = () => {
  const { updateUser } = useAuthStore()
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateProfileRequest) =>
      api.put<UserProfile>('/api/users/profile', data).then(r => r.data),
    onSuccess: data => {
      updateUser(data)
      qc.invalidateQueries({ queryKey: ['user', 'profile'] })
      toast.success('Perfil atualizado com sucesso!')
    },
    onError: () => {
      toast.error('Erro ao atualizar perfil. Tente novamente.')
    },
  })
}

// Change Password // adicionado
export const useChangePassword = () => // adicionado
  useMutation({ // adicionado
    mutationFn: (data: ChangePasswordRequest) => // adicionado
      api.patch('/api/users/change-password', data), // adicionado
    onSuccess: () => { // adicionado
      toast.success('Senha alterada com sucesso!') // adicionado
    }, // adicionado
    onError: (err: any) => { // adicionado
      const errors = err.response?.data?.errors // adicionado
      if (errors) { // adicionado
        const msgs = Object.values(errors).flat().join(' ') // adicionado
        toast.error(msgs) // adicionado
      } else { // adicionado
        toast.error( // adicionado
          err.response?.data?.message ?? 'Erro ao alterar senha. Tente novamente.' // adicionado
        ) // adicionado
      } // adicionado
    }, // adicionado
  }) // adicionado

// Forgot Password
export const useForgotPassword = () =>
  useMutation({
    mutationFn: (data: ForgotPasswordRequest) =>
      api.post('/api/auth/forgot-password', data),
    onSuccess: () => {
      toast.success('Se o email existir, receberá um link em breve.')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error('Erro ao processar solicitação. Tente novamente.')
      }
    },
  })

// Reset Password
export const useResetPassword = () => {
  const navigate = useNavigate()

  return useMutation({
    mutationFn: (data: ResetPasswordRequest) =>
      api.post('/api/auth/reset-password', data),
    onSuccess: () => {
      toast.success('Senha redefinida com sucesso! Faça login para continuar.')
      navigate('/login')
    },
    onError: (err: any) => {
      const errors = err.response?.data?.errors
      if (errors) {
        const msgs = Object.values(errors).flat().join(' ')
        toast.error(msgs)
      } else {
        toast.error(
          err.response?.data?.message ?? 'Token inválido ou expirado.'
        )
      }
    },
  })
}