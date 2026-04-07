import { Download, Loader2, AlertCircle, Clock, CheckCircle2, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import api from '@/lib/axios'
import { ReportStatus } from '../types/report.types'
import { useDeleteReport } from '../api/useReports'
import type { Report } from '../types/report.types'

interface ReportCardProps {
  report: Report
}

const MONTHS = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
]

// usa tokens da paleta
const statusConfig = {
  [ReportStatus.Pending]: {
    label: 'Aguardando',  icon: Clock,
    color: 'var(--ff-pending)',
    bg:    'rgba(245,158,11,0.08)', border: 'rgba(245,158,11,0.2)',
  },
  [ReportStatus.Processing]: {
    label: 'Processando', icon: Loader2,
    color: 'var(--ff-scheduled)',
    bg:    'rgba(99,102,241,0.08)', border: 'rgba(99,102,241,0.2)',
  },
  [ReportStatus.Completed]: {
    label: 'Concluído',   icon: CheckCircle2,
    color: 'var(--ff-income)',
    bg:    'rgba(16,185,129,0.08)', border: 'rgba(16,185,129,0.2)',
  },
  [ReportStatus.Failed]: {
    label: 'Falhou',      icon: AlertCircle,
    color: 'var(--ff-expense)',
    bg:    'rgba(244,63,94,0.08)', border: 'rgba(244,63,94,0.2)',
  },
}

function formatDate(dateStr: string): string {
  const utcStr = dateStr.endsWith('Z') ? dateStr : `${dateStr}Z`
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    timeZone: 'America/Sao_Paulo',
  }).format(new Date(utcStr))
}

export default function ReportCard({ report }: ReportCardProps) {
  const config     = statusConfig[report.status]
  const StatusIcon = config.icon
  const isReady    = report.status === ReportStatus.Completed
  const [confirmDelete, setConfirmDelete] = useState(false)
  const deleteReport = useDeleteReport()

  async function handleDownload() {
    try {
      const response = await api.get(`/api/reports/${report.id}/download`, { responseType: 'blob' })
      const url  = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href     = url
      link.download = report.fileName ?? 'relatorio.csv'
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    } catch {
      toast.error('Erro ao baixar o relatório.')
    }
  }

  return (
    <div
      className="rounded-2xl p-5 flex items-center justify-between gap-4 transition-colors"
      style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
      onMouseEnter={e => (e.currentTarget.style.borderColor = '#333333')}
      onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--ff-border)')}
    >
      {/* Info */}
      <div className="flex items-center gap-4">
        <div
          className="w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0"
          style={{ background: 'var(--ff-emerald-subtle)', border: '1px solid rgba(16,185,129,0.2)' }}
        >
          <span className="text-xs font-bold" style={{ color: 'var(--ff-emerald)' }}>CSV</span>
        </div>
        <div>
          <p className="font-medium text-sm" style={{ color: 'var(--ff-text-primary)' }}>
            Relatório — {MONTHS[report.month - 1]} {report.year}
          </p>
          <p className="text-xs mt-0.5" style={{ color: 'var(--ff-text-muted)' }}>
            Solicitado em {formatDate(report.createdAt)}
            {report.completedAt && (
              <span> · Concluído em {formatDate(report.completedAt)}</span>
            )}
          </p>
        </div>
      </div>

      {/* Status e ações */}
      <div className="flex items-center gap-3 flex-shrink-0">
        <span
          className="flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full"
          style={{ background: config.bg, border: `1px solid ${config.border}`, color: config.color }}
        >
          <StatusIcon
            size={12}
            className={report.status === ReportStatus.Processing ? 'animate-spin' : ''}
          />
          {config.label}
        </span>

        {isReady && report.fileName && (
          <button
            onClick={handleDownload}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-medium transition-colors"
            style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
            onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-emerald-hover)')}
            onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-emerald)')}
          >
            <Download size={12} />
            Baixar CSV
          </button>
        )}

        {!confirmDelete ? (
          <button
            onClick={() => setConfirmDelete(true)}
            className="p-1.5 rounded-lg transition-all"
            style={{ color: 'var(--ff-text-muted)' }}
            onMouseEnter={e => {
              e.currentTarget.style.color = 'var(--ff-expense)'
              e.currentTarget.style.background = 'var(--ff-bg-elevated)'
            }}
            onMouseLeave={e => {
              e.currentTarget.style.color = 'var(--ff-text-muted)'
              e.currentTarget.style.background = 'transparent'
            }}
            title="Remover relatório"
          >
            <Trash2 size={14} />
          </button>
        ) : (
          <div className="flex items-center gap-2">
            <span className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>Confirmar?</span>
            <button
              onClick={() => deleteReport.mutate(report.id, { onSettled: () => setConfirmDelete(false) })}
              disabled={deleteReport.isPending}
              className="text-xs px-2 py-1 rounded-lg transition-colors"
              style={{ background: 'rgba(244,63,94,0.1)', color: 'var(--ff-expense)', border: '1px solid rgba(244,63,94,0.2)' }}
              onMouseEnter={e => (e.currentTarget.style.background = 'rgba(244,63,94,0.2)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'rgba(244,63,94,0.1)')}
            >
              {deleteReport.isPending ? 'Removendo...' : 'Sim'}
            </button>
            <button
              onClick={() => setConfirmDelete(false)}
              className="text-xs px-2 py-1 rounded-lg transition-colors"
              style={{ background: 'var(--ff-bg-elevated)', color: 'var(--ff-text-muted)' }}
              onMouseEnter={e => (e.currentTarget.style.background = '#222222')}
              onMouseLeave={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
            >
              Não
            </button>
          </div>
        )}
      </div>
    </div>
  )
}