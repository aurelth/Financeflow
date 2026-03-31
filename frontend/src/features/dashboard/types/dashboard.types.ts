// Sumário do dashboard
export interface DashboardSummary {
  totalIncome:      number
  totalExpenses:    number
  balance:          number
  projectedBalance: number
  month:            number
  year:             number
}

// Evolução do saldo (gráfico de linha)
export interface BalanceEvolution {
  date:     string
  income:   number
  expenses: number
  balance:  number
}

// Despesas por categoria (gráfico de rosca)
export interface ExpensesByCategory {
  categoryId:    string
  categoryName:  string
  categoryIcon:  string
  categoryColor: string
  total:         number
  percentage:    number
}

// Comparação semanal (gráfico de barras)
export interface WeeklyComparison {
  week:     number
  label:    string
  income:   number
  expenses: number
}

// Filtro de período
export interface DashboardQuery {
  month: number
  year:  number
}

// Comparativo histórico

export interface PeriodData {
  month:         number
  year:          number
  totalIncome:   number
  totalExpenses: number
  balance:       number
}

export interface CategoryComparison {
  categoryId:    string
  categoryName:  string
  categoryIcon:  string
  categoryColor: string
  values:        number[]
  variations:    (number | null)[]
}

export interface PeriodComparison {
  periods:             PeriodData[]
  categoryComparisons: CategoryComparison[]
}

// Filtro para comparativo de períodos
export interface PeriodComparisonQuery {
  periods: string[] // formato YYYY-MM
}