export interface AdminUser {
  id:        string
  name:      string
  email:     string
  cpf:       string
  gender:    string
  role:      'User' | 'Admin'
  currency:  string
  timezone:  string
  isActive:  boolean
  createdAt: string
  deletedAt: string | null
}

export interface AdminUserList {
  users:      AdminUser[]
  total:      number
  page:       number
  pageSize:   number
  totalPages: number
}

export interface AdminCategory {
  id:        string
  name:      string
  icon:      string
  color:     string
  type:      'Expense' | 'Income'
  isActive:  boolean
  createdAt: string
}

export interface CreateDefaultCategoryRequest {
  name:  string
  icon:  string
  color: string
  type:  'Expense' | 'Income'
}

export interface UpdateDefaultCategoryRequest {
  name:  string
  icon:  string
  color: string
}

export interface AdminMetrics {
  totalUsers:        number
  activeUsers:       number
  inactiveUsers:     number
  totalAdmins:       number
  totalCategories:   number
  defaultCategories: number
}