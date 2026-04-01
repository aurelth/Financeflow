// Enums

export enum ReportStatus {
  Pending    = 1,
  Processing = 2,
  Completed  = 3,
  Failed     = 4,
}

export enum ReportType {
  CSV = 1,
}

// Relatório

export interface Report {
  id:          string
  type:        ReportType
  status:      ReportStatus
  month:       number
  year:        number
  fileName:    string | null
  createdAt:   string
  completedAt: string | null
}

// Requests

export interface CreateReportRequest {
  month: number
  year:  number
}