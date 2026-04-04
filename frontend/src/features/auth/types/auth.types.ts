export interface UserProfile {
  id: string
  name: string
  email: string
  cpf:       string
  gender:    string
  currency: string
  timezone: string
  createdAt: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  tokenType: string
  expiresIn: number
  user: UserProfile
}

export interface RegisterRequest {
  name:      string
  email:     string
  password:  string
  cpf:       string
  gender:    string
  currency?: string
  timezone?: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface UpdateProfileRequest {
  currency: string
  timezone: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword:     string
  confirmPassword: string
} 

export interface ValidationErrorResponse {
  status: number
  message: string
  errors: Record<string, string[]>
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token:           string
  newPassword:     string
  confirmPassword: string
}