import { useState } from 'react'
import { FileDown, X } from 'lucide-react'
import { useRequestReport } from '../api/useReports'
import MonthYearPicker from '@/components/ui/MonthYearPicker'

interface ExportButtonProps {
  defaultMonth?: number
  defaultYear?:  number
}

export default function ExportButton({ defaultMonth, defaultYear }: ExportButtonProps) {
  const now          = new Date()
  const initialMonth = defaultMonth ?? now.getMonth() + 1
  const initialYear  = defaultYear  ?? now.getFullYear()

  const [showModal, setShowModal] = useState(false)
  const [month, setMonth]         = useState(initialMonth)
  const [year,  setYear]          = useState(initialYear)

  const requestReport = useRequestReport()

  function handleClose() {
    setMonth(initialMonth)
    setYear(initialYear)
    setShowModal(false)
  }

  return (
    <>
      <button
        onClick={() => setShowModal(true)}
        className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors"
        style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
        onMouseEnter={e => {
          e.currentTarget.style.background = 'var(--ff-bg-elevated)'
          e.currentTarget.style.color = 'var(--ff-text-primary)'
        }}
        onMouseLeave={e => {
          e.currentTarget.style.background = 'transparent'
          e.currentTarget.style.color = 'var(--ff-text-secondary)'
        }}
      >
        <FileDown size={16} />
        Exportar CSV
      </button>

      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
          <div
            className="w-full max-w-sm rounded-2xl shadow-2xl"
            style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
          >
            <div
              className="flex items-center justify-between px-6 py-4"
              style={{ borderBottom: '1px solid var(--ff-border)' }}
            >
              <h2 className="font-semibold text-lg" style={{ color: 'var(--ff-text-primary)' }}>
                Exportar CSV
              </h2>
              <button
                onClick={handleClose}
                className="p-1.5 rounded-lg transition-all"
                style={{ color: 'var(--ff-text-muted)' }}
                onMouseEnter={e => {
                  e.currentTarget.style.color = 'var(--ff-text-primary)'
                  e.currentTarget.style.background = 'var(--ff-bg-elevated)'
                }}
                onMouseLeave={e => {
                  e.currentTarget.style.color = 'var(--ff-text-muted)'
                  e.currentTarget.style.background = 'transparent'
                }}
              >
                <X size={18} />
              </button>
            </div>

            <div className="px-6 py-5 space-y-4">
              <div>
                <label
                  className="text-xs font-medium uppercase tracking-wide mb-2 block"
                  style={{ color: 'var(--ff-text-muted)' }}
                >
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
              <p className="text-xs" style={{ color: 'var(--ff-text-muted)' }}>
                O arquivo CSV será gerado em background. Você receberá uma notificação quando estiver pronto para download.
              </p>
            </div>

            <div
              className="flex gap-3 px-6 py-4"
              style={{ borderTop: '1px solid var(--ff-border)' }}
            >
              <button
                onClick={handleClose}
                className="flex-1 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors"
                style={{ border: '1px solid var(--ff-border)', color: 'var(--ff-text-secondary)' }}
                onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-elevated)')}
                onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
              >
                Cancelar
              </button>
              <button
                onClick={() => requestReport.mutate({ month, year }, { onSuccess: handleClose })}
                disabled={requestReport.isPending}
                className="flex-1 px-4 py-2.5 rounded-xl text-sm font-semibold transition-colors disabled:opacity-50"
                style={{ background: 'var(--ff-emerald)', color: 'var(--ff-emerald-subtle)' }}
                onMouseEnter={e => { if (!requestReport.isPending) e.currentTarget.style.background = 'var(--ff-emerald-hover)' }}
                onMouseLeave={e => { if (!requestReport.isPending) e.currentTarget.style.background = 'var(--ff-emerald)' }}
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