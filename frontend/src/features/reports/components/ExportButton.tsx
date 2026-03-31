import { useState } from 'react'
import { FileDown, X, ChevronLeft, ChevronRight } from 'lucide-react'
import { useRequestReport } from '../api/useReports'

interface ExportButtonProps {
  defaultMonth?: number
  defaultYear?:  number
}

const MONTHS = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
]

export default function ExportButton({
  defaultMonth,
  defaultYear,
}: ExportButtonProps) {
  const now   = new Date()
  const [showModal, setShowModal] = useState(false)
  const [month, setMonth]         = useState(defaultMonth ?? now.getMonth() + 1)
  const [year,  setYear]          = useState(defaultYear  ?? now.getFullYear())

  const requestReport = useRequestReport()

  function handlePrevMonth() {
    const date = new Date(year, month - 2)
    setMonth(date.getMonth() + 1)
    setYear(date.getFullYear())
  }

  function handleNextMonth() {
    const date = new Date(year, month)
    setMonth(date.getMonth() + 1)
    setYear(date.getFullYear())
  }

  function handleRequest() {
    requestReport.mutate({ month, year }, {
      onSuccess: () => setShowModal(false),
    })
  }

  return (
    <>
      <button
        onClick={() => setShowModal(true)}
        className="flex items-center gap-2 px-4 py-2.5 rounded-xl border border-slate-700 text-slate-300 text-sm font-medium hover:bg-slate-800 hover:text-slate-200 transition-colors"
      >
        <FileDown size={16} />
        Exportar CSV
      </button>

      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-sm shadow-2xl">

            {/* Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-800">
              <h2 className="text-slate-100 font-semibold text-lg">Exportar CSV</h2>
              <button
                onClick={() => setShowModal(false)}
                className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-all"
              >
                <X size={18} />
              </button>
            </div>

            {/* Body */}
            <div className="px-6 py-5 space-y-4">
              <div>
                <label className="text-xs font-medium text-slate-400 uppercase tracking-wide mb-2 block">
                  Período
                </label>
                <div className="flex items-center justify-between bg-slate-800 border border-slate-700 rounded-xl px-4 py-2.5">
                  <button
                    onClick={handlePrevMonth}
                    className="p-1 text-slate-400 hover:text-slate-200 transition-colors"
                  >
                    <ChevronLeft size={16} />
                  </button>
                  <span className="text-slate-200 text-sm font-medium">
                    {MONTHS[month - 1]} {year}
                  </span>
                  <button
                    onClick={handleNextMonth}
                    className="p-1 text-slate-400 hover:text-slate-200 transition-colors"
                  >
                    <ChevronRight size={16} />
                  </button>
                </div>
              </div>

              <p className="text-xs text-slate-500">
                O arquivo CSV será gerado em background. Você receberá uma notificação quando estiver pronto para download.
              </p>
            </div>

            {/* Footer */}
            <div className="flex gap-3 px-6 py-4 border-t border-slate-800">
              <button
                onClick={() => setShowModal(false)}
                className="flex-1 px-4 py-2.5 rounded-xl border border-slate-700 text-slate-300 text-sm font-medium hover:bg-slate-800 transition-colors"
              >
                Cancelar
              </button>
              <button
                onClick={handleRequest}
                disabled={requestReport.isPending}
                className="flex-1 px-4 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-semibold transition-colors"
              >
                {requestReport.isPending ? 'Solicitando...' : 'Solicitar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}