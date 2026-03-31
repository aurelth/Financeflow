import { Download, Loader2, AlertCircle, Clock, CheckCircle2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { ReportStatus } from '../types/report.types'
import type { Report } from '../types/report.types'

interface ReportCardProps {
  report: Report
}

const MONTHS = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
]

const statusConfig = {
  [ReportStatus.Pending]:    { label: 'Aguardando',  icon: Clock,        color: 'text-amber-400',   bg: 'bg-amber-500/10 border-amber-500/20'   },
  [ReportStatus.Processing]: { label: 'Processando', icon: Loader2,      color: 'text-indigo-400',  bg: 'bg-indigo-500/10 border-indigo-500/20' },
  [ReportStatus.Completed]:  { label: 'Concluído',   icon: CheckCircle2, color: 'text-emerald-400', bg: 'bg-emerald-500/10 border-emerald-500/20' },
  [ReportStatus.Failed]:     { label: 'Falhou',      icon: AlertCircle,  color: 'text-red-400',     bg: 'bg-red-500/10 border-red-500/20' },
}

export default function ReportCard({ report }: ReportCardProps) {
  const config     = statusConfig[report.status]
  const StatusIcon = config.icon
  const isReady    = report.status === ReportStatus.Completed
  const downloadUrl = `/api/reports/${report.id}/download`

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 flex items-center justify-between gap-4 hover:border-slate-700 transition-colors">
      <div className="flex items-center gap-4">
        <div className="w-10 h-10 rounded-xl bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-center flex-shrink-0">
          <span className="text-indigo-400 text-xs font-bold">CSV</span>
        </div>
        <div>
          <p className="text-slate-200 font-medium text-sm">
            Relatório — {MONTHS[report.month - 1]} {report.year}
          </p>
          <p className="text-slate-500 text-xs mt-0.5">
            Solicitado em {new Date(report.createdAt).toLocaleDateString('pt-BR')}
            {report.completedAt && (
              <span> · Concluído em {new Date(report.completedAt).toLocaleDateString('pt-BR')}</span>
            )}
          </p>
        </div>
      </div>

      <div className="flex items-center gap-3 flex-shrink-0">
        <span className={cn('flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full border', config.bg, config.color)}>
          <StatusIcon size={12} className={report.status === ReportStatus.Processing ? 'animate-spin' : ''} />
          {config.label}
        </span>
        {isReady && report.fileName && (
          <a href={downloadUrl} download={report.fileName} className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-medium transition-colors">
            <Download size={12} />
            Baixar CSV
          </a>
        )}
      </div>
    </div>
  )
}