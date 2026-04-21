export interface GoalProgressResultDto {
  id:                  string
  name:                string
  emoji:               string
  targetAmount:        number
  monthlyContribution: number
  deadline:            string
  accumulatedAmount:   number
  plannedThisMonth:    number
  receivedThisMonth:   number
  progressPercentage:  number
  isCompleted:         boolean
  monthsToComplete:    number | null
  status:              'OnTrack' | 'Behind' | 'Completed' | 'Overdue'
}

export interface GoalsSummaryResultDto {
  availableThisMonth: number
  committedThisMonth: number
  difference:         number
  goals:              GoalProgressResultDto[]
}

export interface CreateGoalRequest {
  name:                string
  targetAmount:        number
  monthlyContribution: number
  deadline:            string
  emoji:               string
}

export interface UpdateGoalRequest {
  name:                string
  targetAmount:        number
  monthlyContribution: number
  deadline:            string
  emoji:               string
}