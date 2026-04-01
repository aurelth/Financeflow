import { useState, useRef } from 'react'
import { FileText, X, Loader2 } from 'lucide-react'
import { createPortal } from 'react-dom'
import { useDashboardSummary } from '@/features/dashboard/api/useDashboard'
import { useTransactions } from '@/features/transactions/api/useTransactions'
import MonthYearPicker from '@/components/ui/MonthYearPicker'
import ReportTemplate from './ReportTemplate'

interface PdfExportButtonProps {
  defaultMonth?: number
  defaultYear?:  number
}

export default function PdfExportButton({
  defaultMonth,
  defaultYear,
}: PdfExportButtonProps) {
  const now         = new Date()
  const [showModal,    setShowModal]    = useState(false)
  const [month,        setMonth]        = useState(defaultMonth ?? now.getMonth() + 1)
  const [year,         setYear]         = useState(defaultYear  ?? now.getFullYear())
  const [generating,   setGenerating]   = useState(false)
  const templateRef = useRef<HTMLDivElement>(null)

  const dateFrom = new Date(year, month - 1, 1).toISOString().split('T')[0]
  const dateTo   = new Date(year, month, 0).toISOString().split('T')[0]

  const { data: summary }  = useDashboardSummary({ month, year })
  const { data: txData }   = useTransactions({ page: 1, pageSize: 1000, dateFrom, dateTo })
  const transactions        = txData?.items ?? []

  async function handleGenerate() {
    if (!templateRef.current) return
    setGenerating(true)

    try {
      const html2pdf = (await import('html2pdf.js')).default
      const options  = {
        margin:      [10, 10, 10, 10] as [number, number, number, number],
        filename:    `FinanceFlow_${month}_${year}.pdf`,
        image:       { type: 'jpeg' as const, quality: 0.98 },
        html2canvas: { scale: 2, useCORS: true },
        jsPDF:       { unit: 'mm', format: 'a4', orientation: 'portrait' as const },
      }
      await html2pdf().set(options).from(templateRef.current).save()
      setShowModal(false)
    } catch (err) {
      console.error('Erro ao gerar PDF:', err)
    } finally {
      setGenerating(false)
    }
  }

  return (
    <>
      <button
        onClick={() => setShowModal(true)}
        className="flex items-center gap-2 px-4 py-2.5 rounded-xl border border-slate-700 text-slate-300 text-sm font-medium hover:bg-slate-800 hover:text-slate-200 transition-colors"
      >
        <FileText size={16} />
        Exportar PDF
      </button>

      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-sm shadow-2xl">

            {/* Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-800">
              <h2 className="text-slate-100 font-semibold text-lg">Exportar PDF</h2>
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
                <MonthYearPicker
                  month={month}
                  year={year}
                  onChange={(m, y) => { setMonth(m); setYear(y) }}
                  maxMonth={now.getMonth() + 1}
                  maxYear={now.getFullYear()}
                />
              </div>
              <p className="text-xs text-slate-500">
                O PDF será gerado diretamente no seu navegador com todas as transações do período selecionado.
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
                onClick={handleGenerate}
                disabled={generating}
                className="flex-1 px-4 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-semibold transition-colors flex items-center justify-center gap-2"
              >
                {generating
                  ? <><Loader2 size={14} className="animate-spin" /> Gerando...</>
                  : 'Gerar PDF'
                }
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Template oculto */}
      {showModal && createPortal(
        <div style={{ position: 'absolute', left: '-9999px', top: 0 }}>
          <ReportTemplate
            ref={templateRef}
            month={month}
            year={year}
            summary={summary}
            transactions={transactions}
          />
        </div>,
        document.body
      )}
    </>
  )
}