export interface UserProfile {
  id: string
  name: string
  email: string
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
  name: string
  email: string
  password: string
  currency?: string
  timezone?: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface UpdateProfileRequest {
  name: string
  currency: string
  timezone: string
}

export interface ValidationErrorResponse {
  status: number
  message: string
  errors: Record<string, string[]>
}