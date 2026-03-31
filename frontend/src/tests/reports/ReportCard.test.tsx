import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import ReportCard from '@/features/reports/components/ReportCard'
import { ReportStatus, ReportType } from '@/features/reports/types/report.types'
import type { Report } from '@/features/reports/types/report.types'

const mockReportCompleted: Report = {
  id:          'report-1',
  type:        ReportType.CSV,
  status:      ReportStatus.Completed,
  month:       3,
  year:        2026,
  fileName:    'relatorio_2026_03.csv',
  createdAt:   '2026-03-01T00:00:00Z',
  completedAt: '2026-03-01T00:01:00Z',
}

const mockReportPending: Report = {
  id:          'report-2',
  type:        ReportType.CSV,
  status:      ReportStatus.Pending,
  month:       2,
  year:        2026,
  fileName:    null,
  createdAt:   '2026-02-01T00:00:00Z',
  completedAt: null,
}

const mockReportFailed: Report = {
  id:          'report-3',
  type:        ReportType.CSV,
  status:      ReportStatus.Failed,
  month:       1,
  year:        2026,
  fileName:    null,
  createdAt:   '2026-01-01T00:00:00Z',
  completedAt: null,
}

const renderCard = (report: Report) => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ReportCard report={report} />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('ReportCard', () => {
  it('deve renderizar o período do relatório', () => {
    renderCard(mockReportCompleted)
    expect(screen.getByText('Relatório — Março 2026')).toBeInTheDocument()
  })

  it('deve exibir status Concluído', () => {
    renderCard(mockReportCompleted)
    expect(screen.getByText('Concluído')).toBeInTheDocument()
  })

  it('deve exibir status Aguardando', () => {
    renderCard(mockReportPending)
    expect(screen.getByText('Aguardando')).toBeInTheDocument()
  })

  it('deve exibir status Falhou', () => {
    renderCard(mockReportFailed)
    expect(screen.getByText('Falhou')).toBeInTheDocument()
  })

  it('deve exibir botão de download quando relatório está concluído', () => {
    renderCard(mockReportCompleted)
    expect(screen.getByText('Baixar CSV')).toBeInTheDocument()
  })

  it('não deve exibir botão de download quando relatório está pendente', () => {
    renderCard(mockReportPending)
    expect(screen.queryByText('Baixar CSV')).not.toBeInTheDocument()
  })

  it('deve exibir a data de conclusão quando disponível', () => {
    renderCard(mockReportCompleted)
    expect(screen.getByText(/Concluído em/i)).toBeInTheDocument()
  })
})