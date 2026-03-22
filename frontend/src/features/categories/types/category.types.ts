// Enums

export enum TransactionType {
  Income  = 0,
  Expense = 1,
}

// Subcategoria

export interface Subcategory {
  id:       string
  name:     string
  isActive: boolean
}

// Categoria

export interface Category {
  id:            string
  name:          string
  icon:          string
  color:         string
  type:          TransactionType
  isDefault:     boolean
  isActive:      boolean
  isOwner:       boolean
  subcategories: Subcategory[]
}

// Requests
export interface CreateCategoryRequest {
  name:  string
  icon:  string
  color: string
  type:  TransactionType
}

export interface UpdateCategoryRequest {
  name:  string
  icon:  string
  color: string
}

export interface CreateSubcategoryRequest {
  name: string
}

export interface UpdateSubcategoryRequest {
  name: string
}