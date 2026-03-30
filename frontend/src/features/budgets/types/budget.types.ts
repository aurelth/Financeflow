// Orçamento

export interface Budget {
  id:            string
  categoryId:    string
  categoryName:  string
  categoryIcon:  string
  categoryColor: string
  month:         number
  year:          number
  limitAmount:   number
  createdAt:     string
  updatedAt:     string | null
}

// Resumo de orçamento (com gasto atual)

export interface BudgetSummary {
  id:            string
  categoryId:    string
  categoryName:  string
  categoryIcon:  string
  categoryColor: string
  month:         number
  year:          number
  limitAmount:   number
  spentAmount:   number
  percentage:    number
}

// Requests

export interface CreateBudgetRequest {
  categoryId:  string
  month:       number
  year:        number
  limitAmount: number
}

export interface UpdateBudgetRequest {
  limitAmount: number
}

export interface GetBudgetsQuery {
  month: number
  year:  number
}