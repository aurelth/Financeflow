export interface ScoreDetail {
  criterion:    string
  points:       number
  maxPoints:    number
  justification: string
}

export interface HealthScoreResult {
  score:          number
  classification: string
  details:        ScoreDetail[]
}

export interface HealthScoreHistoryItem {
  month:          number
  year:           number
  monthLabel:     string
  score:          number
  classification: string
}