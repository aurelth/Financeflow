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

// Utilitário para extrair mensagem de erro da API
const getApiError = (err: any, fallback: string): string => {
  const errors = err?.response?.data?.errors
  if (errors) return Object.values(errors).flat().join(' ')
  return err?.response?.data?.message ?? err?.response?.data?.title ?? fallback
}

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
      toast.error(getApiError(err, 'Erro ao criar conta. Tente novamente.'))
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
    onError: (err: any) => {
      toast.error(getApiError(err, 'Email ou senha incorreto.'))
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
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao atualizar perfil. Tente novamente.'))
    },
  })
}

// Change Password
export const useChangePassword = () =>
  useMutation({
    mutationFn: (data: ChangePasswordRequest) =>
      api.patch('/api/users/change-password', data),
    onSuccess: () => {
      toast.success('Senha alterada com sucesso!')
    },
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao alterar senha. Tente novamente.'))
    },
  })

// Forgot Password
export const useForgotPassword = () =>
  useMutation({
    mutationFn: (data: ForgotPasswordRequest) =>
      api.post('/api/auth/forgot-password', data),
    onSuccess: () => {
      toast.success('Se o email existir, receberá um link em breve.')
    },    
    onError: (err: any) => {
      toast.error(getApiError(err, 'Erro ao processar solicitação. Tente novamente.'))
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
      toast.error(getApiError(err, 'Token inválido ou expirado.'))
    },
  })
}