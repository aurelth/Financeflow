import { Loader2 } from 'lucide-react'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer,
} from 'recharts'
import { useHealthScore, useHealthScoreHistory } from '../api/useHealthScore'
import type { ScoreDetail } from '../types/healthscore.types'

// Cores por classificação
function getScoreColor(score: number): string {
  if (score >= 80) return '#10b981' // verde — Excelente
  if (score >= 60) return '#6366f1' // índigo — Bom
  if (score >= 40) return '#f59e0b' // amarelo — Regular
  if (score >= 20) return '#f97316' // laranja — Atenção
  return '#f43f5e'                  // vermelho — Crítico
}

function getScoreBg(score: number): string {
  if (score >= 80) return 'rgba(16,185,129,0.1)'
  if (score >= 60) return 'rgba(99,102,241,0.1)'
  if (score >= 40) return 'rgba(245,158,11,0.1)'
  if (score >= 20) return 'rgba(249,115,22,0.1)'
  return 'rgba(244,63,94,0.1)'
}

// Gauge semicircular em SVG
function ScoreGauge({ score }: { score: number }) {
  const color      = getScoreColor(score)
  const radius     = 80
  const cx         = 110
  const cy         = 110
  const startAngle = 180
  const endAngle   = startAngle + (score / 100) * 180

  function polarToCartesian(angle: number) {
    const rad = (angle * Math.PI) / 180
    return {
      x: cx + radius * Math.cos(rad),
      y: cy + radius * Math.sin(rad),
    }
  }

  const start = polarToCartesian(startAngle)
  const end   = polarToCartesian(endAngle)
  const large = endAngle - startAngle > 180 ? 1 : 0

  const trackStart = polarToCartesian(180)
  const trackEnd   = polarToCartesian(360)

  return (
    <svg width="220" height="130" viewBox="0 0 220 130">
      {/* Trilho de fundo */}
      <path
        d={`M ${trackStart.x} ${trackStart.y} A ${radius} ${radius} 0 0 1 ${trackEnd.x} ${trackEnd.y}`}
        fill="none"
        stroke="var(--ff-border)"
        strokeWidth="16"
        strokeLinecap="round"
      />
      {/* Arco do score */}
      {score > 0 && (
        <path
          d={`M ${start.x} ${start.y} A ${radius} ${radius} 0 ${large} 1 ${end.x} ${end.y}`}
          fill="none"
          stroke={color}
          strokeWidth="16"
          strokeLinecap="round"
        />
      )}
      {/* Score no centro */}
      <text
        x={cx}
        y={cy - 8}
        textAnchor="middle"
        fontSize="36"
        fontWeight="700"
        fill={color}
      >
        {score}
      </text>
      <text
        x={cx}
        y={cy + 14}
        textAnchor="middle"
        fontSize="13"
        fill="var(--ff-text-muted)"
      >
        de 100
      </text>
      {/* Labels */}
      <text x="18" y="122" fontSize="11" fill="var(--ff-text-muted)">0</text>
      <text x="196" y="122" fontSize="11" fill="var(--ff-text-muted)">100</text>
    </svg>
  )
}

// Card de critério
function CriterionCard({ detail }: { detail: ScoreDetail }) {
  const percentage = (detail.points / detail.maxPoints) * 100
  const color      = getScoreColor(percentage)

  return (
    <div
      className="rounded-2xl p-4"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
    >
      <div className="flex items-center justify-between mb-2">
        <span className="text-sm font-medium" style={{ color: 'var(--ff-text-primary)' }}>
          {detail.criterion}
        </span>
        <span className="text-sm font-semibold" style={{ color }}>
          {detail.points}/{detail.maxPoints} pts
        </span>
      </div>
      {/* Barra de progresso */}
      <div
        className="h-1.5 rounded-full mb-2"
        style={{ background: 'var(--ff-border)' }}
      >
        <div
          className="h-1.5 rounded-full transition-all duration-500"
          style={{ width: `${percentage}%`, background: color }}
        />
      </div>
      <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
        {detail.justification}
      </p>
    </div>
  )
}

export default function HealthScorePage() {
  const { data: score,   isLoading: l1 } = useHealthScore()
  const { data: history, isLoading: l2 } = useHealthScoreHistory()

  const isLoading = l1 || l2

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div>
        <h1 className="text-xl font-semibold" style={{ color: 'var(--ff-text-primary)' }}>
          Saúde Financeira
        </h1>
        <p className="text-sm mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
          Avaliação das suas finanças com base nos dados do mês atual
        </p>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
        </div>
      )}

      {!isLoading && score && (
        <div className="space-y-6">

          {/* Score principal */}
          <div
            className="rounded-2xl p-6 flex flex-col items-center gap-2"
            style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
          >
            <ScoreGauge score={score.score} />
            <div
              className="px-4 py-1.5 rounded-full text-sm font-semibold"
              style={{
                background: getScoreBg(score.score),
                color:      getScoreColor(score.score),
              }}
            >
              {score.classification}
            </div>
            <p className="text-xs text-center max-w-sm" style={{ color: 'var(--ff-text-muted)' }}>
              Score calculado com base no saldo do mês, controlo de orçamentos, receitas, diversificação de despesas e transações agendadas.
            </p>
          </div>

          {/* Breakdown por critério */}
          <div>
            <h2 className="text-sm font-semibold mb-3 uppercase tracking-wide" style={{ color: 'var(--ff-text-muted)' }}>
              Detalhes por critério
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
              {score.details.map(detail => (
                <CriterionCard key={detail.criterion} detail={detail} />
              ))}
            </div>
          </div>

          {/* Gráfico de evolução histórica */}
          {history && history.length > 0 && (
            <div
              className="rounded-2xl p-5"
              style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
            >
              <h2 className="text-sm font-semibold mb-4" style={{ color: 'var(--ff-text-primary)' }}>
                Evolução dos últimos 6 meses
              </h2>
              <ResponsiveContainer width="100%" height={200}>
                <LineChart data={history} margin={{ top: 4, right: 8, left: -20, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--ff-border)" />
                  <XAxis
                    dataKey="monthLabel"
                    tick={{ fontSize: 12, fill: 'var(--ff-text-muted)' }}
                    axisLine={false}
                    tickLine={false}
                  />
                  <YAxis
                    domain={[0, 100]}
                    tick={{ fontSize: 12, fill: 'var(--ff-text-muted)' }}
                    axisLine={false}
                    tickLine={false}
                  />
                  <Tooltip
                    contentStyle={{
                      background:   'var(--ff-bg-elevated)',
                      border:       '1px solid var(--ff-border)',
                      borderRadius: '12px',
                      fontSize:     '13px',
                      color:        'var(--ff-text-primary)',
                    }}
                    formatter={(value: any) => [`${value} pts`, 'Score']}
                  />
                  <Line
                    type="monotone"
                    dataKey="score"
                    stroke="var(--ff-emerald)"
                    strokeWidth={2}
                    dot={{ fill: 'var(--ff-emerald)', strokeWidth: 0, r: 4 }}
                    activeDot={{ r: 6 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          )}

        </div>
      )}
    </div>
  )
}