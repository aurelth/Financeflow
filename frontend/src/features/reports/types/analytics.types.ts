// Fluxo de Caixa
export interface CashFlowPeriodDto {
  label:             string
  income:            number
  expenses:          number
  balance:           number
  cumulativeBalance: number
}

export interface CashFlowDto {
  from:          string
  to:            string
  groupBy:       string
  periods:       CashFlowPeriodDto[]
  totalIncome:   number
  totalExpenses: number
  netBalance:    number
}

// Resumo Anual
export interface AnnualMonthDto {
  month:             number
  monthName:         string
  income:            number
  expenses:          number
  balance:           number
  cumulativeBalance: number
}

export interface AnnualSummaryDto {
  year:                    number
  months:                  AnnualMonthDto[]
  totalIncome:             number
  totalExpenses:           number
  netBalance:              number
  averageMonthlyIncome:    number
  averageMonthlyExpenses:  number
}

// Por Categoria
export interface SubcategoryReportItemDto {
  subcategoryId:   string
  subcategoryName: string
  amount:          number
  percentage:      number
  transactionCount: number
}

export interface CategoryReportItemDto {
  categoryId:       string
  categoryName:     string
  categoryIcon:     string
  categoryColor:    string
  type:             'Income' | 'Expense'
  amount:           number
  percentage:       number
  transactionCount: number
  subcategories:    SubcategoryReportItemDto[]
}

export interface ReportByCategoryDto {
  from:          string
  to:            string
  totalExpenses: number
  totalIncome:   number
  categories:    CategoryReportItemDto[]
}

// Por Tag
export interface TagReportItemDto {
  tag:              string
  amount:           number
  percentage:       number
  transactionCount: number
}

export interface ReportByTagDto {
  from:        string
  to:          string
  totalAmount: number
  tags:        TagReportItemDto[]
}

// Projecções
export interface ProjectionMonthDto {
  year:        number
  month:       number
  monthName:   string
  income:      number
  expenses:    number
  balance:     number
  isProjected: boolean
}

export interface ProjectionsDto {
  monthsAnalysed: number
  monthsAhead:    number
  historical:     ProjectionMonthDto[]
  projected:      ProjectionMonthDto[]
}

// Parâmetros de queries
export interface CashFlowParams {
  from:    string
  to:      string
  groupBy: 'day' | 'month'
}

export interface ByCategoryParams {
  from:  string
  to:    string
  type?: 'Income' | 'Expense'
}

export interface ByTagParams {
  from: string
  to:   string
}

export interface ProjectionsParams {
  monthsBack:  number
  monthsAhead: number
}