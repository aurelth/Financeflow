import { FileText, Loader2 } from 'lucide-react'
import { useReports } from '../api/useReports'
import ReportCard from '../components/ReportCard'
import ExportButton from '../components/ExportButton'
import PdfExportButton from '../components/PdfExportButton'

export default function ExportsPage() {
  const { data: reports = [], isLoading } = useReports()

  const isEmpty = !isLoading && reports.length === 0

  return (
    <div className="space-y-6">

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-white">Exportações</h1>
          <p className="text-slate-400 text-sm mt-0.5">
            Exporte seus dados financeiros em CSV ou PDF
          </p>
        </div>
        <div className="flex items-center gap-2">
          <PdfExportButton />
          <ExportButton />
        </div>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={24} className="animate-spin text-indigo-400" />
        </div>
      )}

      {/* Lista de relatórios */}
      {!isLoading && !isEmpty && (
        <div className="space-y-3">
          {reports.map(report => (
            <ReportCard key={report.id} report={report} />
          ))}
        </div>
      )}

      {/* Estado vazio */}
      {isEmpty && (
        <div className="flex flex-col items-center justify-center py-20 space-y-3">
          <div className="w-14 h-14 bg-slate-800 rounded-2xl flex items-center justify-center">
            <FileText size={24} className="text-slate-500" />
          </div>
          <p className="text-slate-400 text-sm">
            Nenhum relatório gerado ainda
          </p>
          <div className="flex items-center gap-2">
            <PdfExportButton />
            <ExportButton />
          </div>
        </div>
      )}
    </div>
  )
}