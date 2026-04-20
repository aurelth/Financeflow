import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import HealthScorePage from '@/features/healthscore/pages/HealthScorePage'
import type { HealthScoreResult, HealthScoreHistoryItem } from '@/features/healthscore/types/healthscore.types'

const mockScore: HealthScoreResult = {
  score:          75,
  classification: 'Bom',
  details: [
    { criterion: 'Saldo do mês',               points: 20, maxPoints: 25, justification: 'Saldo positivo de R$ 500,00.'          },
    { criterion: 'Controlo de orçamentos',     points: 25, maxPoints: 25, justification: 'Todas as categorias dentro do limite.' },
    { criterion: 'Regularidade de receitas',   points: 20, maxPoints: 20, justification: 'Receitas de R$ 3.000,00 registadas.'   },
    { criterion: 'Diversificação de despesas', points: 10, maxPoints: 15, justification: 'Gastos moderadamente concentrados.'    },
    { criterion: 'Transações agendadas',       points: 15, maxPoints: 15, justification: 'Nenhuma transação em atraso.'          },
  ],
}

const mockHistory: HealthScoreHistoryItem[] = [
  { month: 11, year: 2025, monthLabel: 'nov/25', score: 60, classification: 'Bom'     },
  { month: 12, year: 2025, monthLabel: 'dez/25', score: 55, classification: 'Regular' },
  { month: 1,  year: 2026, monthLabel: 'jan/26', score: 65, classification: 'Bom'     },
  { month: 2,  year: 2026, monthLabel: 'fev/26', score: 70, classification: 'Bom'     },
  { month: 3,  year: 2026, monthLabel: 'mar/26', score: 72, classification: 'Bom'     },
  { month: 4,  year: 2026, monthLabel: 'abr/26', score: 75, classification: 'Bom'     },
]

// Mock no topo — padrão correto para Vitest
vi.mock('@/features/healthscore/api/useHealthScore', () => ({
  useHealthScore:        () => ({ data: mockScore,   isLoading: false }),
  useHealthScoreHistory: () => ({ data: mockHistory, isLoading: false }),
}))

// Mock do Recharts — não renderiza no jsdom
vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: any) => <div>{children}</div>,
  LineChart:           ({ children }: any) => <div data-testid="line-chart">{children}</div>,
  Line:                () => null,
  XAxis:               ({ data }: any) => <div>{data}</div>,
  YAxis:               () => null,
  CartesianGrid:       () => null,
  Tooltip:             () => null,
}))

const renderPage = () => {
  const qc = new QueryClient()
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <HealthScorePage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('HealthScorePage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('deve renderizar o título da página', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Saúde Financeira')
  })

  it('deve exibir o score principal', () => {
    renderPage()
    expect(screen.getByText('75')).toBeInTheDocument()
  })

  it('deve exibir a classificação do score', () => {
    renderPage()
    expect(screen.getByText('Bom')).toBeInTheDocument()
  })

  it('deve exibir todos os critérios de avaliação', () => {
    renderPage()
    expect(screen.getByText('Saldo do mês')).toBeInTheDocument()
    expect(screen.getByText('Controlo de orçamentos')).toBeInTheDocument()
    expect(screen.getByText('Regularidade de receitas')).toBeInTheDocument()
    expect(screen.getByText('Diversificação de despesas')).toBeInTheDocument()
    expect(screen.getByText('Transações agendadas')).toBeInTheDocument()
  })

  it('deve exibir a pontuação de cada critério', () => {
    renderPage()
    expect(screen.getByText('20/25 pts')).toBeInTheDocument()
    expect(screen.getByText('25/25 pts')).toBeInTheDocument()
    expect(screen.getByText('20/20 pts')).toBeInTheDocument()
    expect(screen.getByText('10/15 pts')).toBeInTheDocument()
    expect(screen.getByText('15/15 pts')).toBeInTheDocument()
  })

  it('deve exibir a justificativa de cada critério', () => {
    renderPage()
    expect(screen.getByText('Saldo positivo de R$ 500,00.')).toBeInTheDocument()
    expect(screen.getByText('Nenhuma transação em atraso.')).toBeInTheDocument()
  })

  it('deve exibir o gráfico de evolução histórica', () => {
    renderPage()
    expect(screen.getByText('Evolução dos últimos 6 meses')).toBeInTheDocument()
    expect(screen.getByTestId('line-chart')).toBeInTheDocument()
  })

  it('deve exibir o texto descritivo do score', () => {
    renderPage()
    expect(screen.getByText(/Score calculado com base/i)).toBeInTheDocument()
  })
})