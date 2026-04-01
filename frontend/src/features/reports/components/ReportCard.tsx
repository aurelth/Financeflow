import { Download, Loader2, AlertCircle, Clock, CheckCircle2, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
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

const statusConfig = {
  [ReportStatus.Pending]:    { label: 'Aguardando',  icon: Clock,        color: 'text-amber-400',   bg: 'bg-amber-500/10 border-amber-500/20'    },
  [ReportStatus.Processing]: { label: 'Processando', icon: Loader2,      color: 'text-indigo-400',  bg: 'bg-indigo-500/10 border-indigo-500/20'  },
  [ReportStatus.Completed]:  { label: 'Concluído',   icon: CheckCircle2, color: 'text-emerald-400', bg: 'bg-emerald-500/10 border-emerald-500/20' },
  [ReportStatus.Failed]:     { label: 'Falhou',      icon: AlertCircle,  color: 'text-red-400',     bg: 'bg-red-500/10 border-red-500/20'         },
}

function formatDate(dateStr: string): string {
  const utcStr = dateStr.endsWith('Z') ? dateStr : `${dateStr}Z`
  return new Intl.DateTimeFormat('pt-BR', {
    day:      '2-digit',
    month:    '2-digit',
    year:     'numeric',
    timeZone: 'America/Sao_Paulo',
  }).format(new Date(utcStr))
}

export default function ReportCard({ report }: ReportCardProps) {
  const config      = statusConfig[report.status]
  const StatusIcon  = config.icon
  const isReady     = report.status === ReportStatus.Completed
  const [confirmDelete, setConfirmDelete] = useState(false)
  const deleteReport = useDeleteReport()

  async function handleDownload() {
    try {
      const response = await api.get(`/api/reports/${report.id}/download`, {
        responseType: 'blob',
      })
      const url  = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href      = url
      link.download  = report.fileName ?? 'relatorio.csv'
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    } catch {
      toast.error('Erro ao baixar o relatório.')
    }
  }

  function handleDeleteClick() {
    setConfirmDelete(true)
  }

  function handleConfirmDelete() {
    deleteReport.mutate(report.id, {
      onSettled: () => setConfirmDelete(false),
    })
  }

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 flex items-center justify-between gap-4 hover:border-slate-700 transition-colors">

      {/* Info */}
      <div className="flex items-center gap-4">
        <div className="w-10 h-10 rounded-xl bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-center flex-shrink-0">
          <span className="text-indigo-400 text-xs font-bold">CSV</span>
        </div>
        <div>
          <p className="text-slate-200 font-medium text-sm">
            Relatório — {MONTHS[report.month - 1]} {report.year}
          </p>
          <p className="text-slate-500 text-xs mt-0.5">
            Solicitado em {formatDate(report.createdAt)}
            {report.completedAt && (
              <span> · Concluído em {formatDate(report.completedAt)}</span>
            )}
          </p>
        </div>
      </div>

      {/* Status e ações */}
      <div className="flex items-center gap-3 flex-shrink-0">
        <span className={cn(
          'flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full border',
          config.bg, config.color
        )}>
          <StatusIcon
            size={12}
            className={report.status === ReportStatus.Processing ? 'animate-spin' : ''}
          />
          {config.label}
        </span>

        {isReady && report.fileName && (
          <button
            onClick={handleDownload}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-medium transition-colors"
          >
            <Download size={12} />
            Baixar CSV
          </button>
        )}

        {/* Botão de excluir */}
        {!confirmDelete ? (
          <button
            onClick={handleDeleteClick}
            className="p-1.5 text-slate-500 hover:text-red-400 hover:bg-slate-800 rounded-lg transition-all"
            title="Remover relatório"
          >
            <Trash2 size={14} />
          </button>
        ) : (
          <div className="flex items-center gap-2">
            <span className="text-xs text-slate-400">Confirmar?</span>
            <button
              onClick={handleConfirmDelete}
              disabled={deleteReport.isPending}
              className="text-xs px-2 py-1 rounded-lg bg-red-500/10 text-red-400 hover:bg-red-500/20 border border-red-500/20 transition-colors"
            >
              {deleteReport.isPending ? 'Removendo...' : 'Sim'}
            </button>
            <button
              onClick={() => setConfirmDelete(false)}
              className="text-xs px-2 py-1 rounded-lg bg-slate-800 text-slate-400 hover:bg-slate-700 transition-colors"
            >
              Não
            </button>
          </div>
        )}
      </div>
    </div>
  )
}