import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import ExportsPage from '@/features/reports/pages/ExportsPage'
import { ReportStatus, ReportType } from '@/features/reports/types/report.types'

const mockReports = [
  {
    id:          'report-1',
    type:        ReportType.CSV,
    status:      ReportStatus.Completed,
    month:       3,
    year:        2026,
    fileName:    'relatorio_2026_03.csv',
    createdAt:   '2026-03-01T00:00:00Z',
    completedAt: '2026-03-01T00:01:00Z',
  },
  {
    id:          'report-2',
    type:        ReportType.CSV,
    status:      ReportStatus.Pending,
    month:       2,
    year:        2026,
    fileName:    null,
    createdAt:   '2026-02-01T00:00:00Z',
    completedAt: null,
  },
]

vi.mock('@/features/reports/api/useReports', () => ({
  useReports:           () => ({ data: mockReports, isLoading: false }),
  useRequestReport:     () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteReport:      () => ({ mutate: vi.fn(), isPending: false }),
  getReportDownloadUrl: (id: string) => `/api/reports/${id}/download`,
}))

vi.mock('@/features/dashboard/api/useDashboard', () => ({
  useDashboardSummary: () => ({ data: undefined, isLoading: false }),
}))

vi.mock('@/features/transactions/api/useTransactions', () => ({
  useTransactions: () => ({ data: { items: [], totalPages: 1 }, isLoading: false }),
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ExportsPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('ExportsPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Exportações')
  })

  it('deve exibir a lista de relatórios', () => {
    renderPage()
    expect(screen.getByText('Relatório — Março 2026')).toBeInTheDocument()
    expect(screen.getByText('Relatório — Fevereiro 2026')).toBeInTheDocument()
  })

  it('deve exibir botões de exportação', () => {
    renderPage()
    expect(screen.getByText('Exportar PDF')).toBeInTheDocument()
    expect(screen.getByText('Exportar CSV')).toBeInTheDocument()
  })

  it('deve exibir status dos relatórios', () => {
    renderPage()
    expect(screen.getByText('Concluído')).toBeInTheDocument()
    expect(screen.getByText('Aguardando')).toBeInTheDocument()
  })

  it('deve abrir modal ao clicar em Exportar CSV', async () => {
    renderPage()
    const user = userEvent.setup()
    await user.click(screen.getByText('Exportar CSV'))
    await waitFor(() => {
      expect(screen.getByText('Solicitar')).toBeInTheDocument()
    })
  })

  it('deve abrir modal ao clicar em Exportar PDF', async () => {
    renderPage()
    const user = userEvent.setup()
    await user.click(screen.getByText('Exportar PDF'))
    await waitFor(() => {
      expect(screen.getByText('Gerar PDF')).toBeInTheDocument()
    })
  })
})