import { useState, useRef, useEffect } from 'react'
import { Paperclip, Eye, Download, X, FileText, Loader2, ZoomIn, ZoomOut } from 'lucide-react'
import api from '@/lib/axios'
import { getAttachmentUrl } from '../api/useTransactions'

interface AttachmentViewerProps {
  transactionId: string
  fileName:      string
  triggerIcon?:  'paperclip' | 'file'
}

const MIN_ZOOM  = 25
const MAX_ZOOM  = 300
const ZOOM_STEP = 25

export default function AttachmentViewer({
  transactionId,
  fileName,
  triggerIcon = 'paperclip',
}: AttachmentViewerProps) {
  const [dropdownOpen, setDropdownOpen] = useState(false)
  const [openUpward, setOpenUpward]     = useState(false)
  const [previewOpen, setPreviewOpen]   = useState(false)
  const [loading, setLoading]           = useState(false)
  const [objectUrl, setObjectUrl]       = useState<string | null>(null)
  const [contentType, setContentType]   = useState<string>('')
  const [zoom, setZoom]                 = useState(100)
  const dropdownRef                     = useRef<HTMLDivElement>(null)
  const triggerRef                      = useRef<HTMLButtonElement>(null)

  const url = getAttachmentUrl(transactionId)

  // Cleanup do object URL ao fechar o modal
  useEffect(() => {
    if (!previewOpen && objectUrl) {
      URL.revokeObjectURL(objectUrl)
      setObjectUrl(null)
      setZoom(100)
    }
  }, [previewOpen])

  // Fecha o dropdown ao clicar fora
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setDropdownOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  function handleToggleDropdown() {
    if (!dropdownOpen && triggerRef.current) {
      const rect       = triggerRef.current.getBoundingClientRect()
      const spaceBelow = window.innerHeight - rect.bottom
      setOpenUpward(spaceBelow < 100)
    }
    setDropdownOpen(o => !o)
  }

  async function handleVisualize() {
    setDropdownOpen(false)
    setPreviewOpen(true)
    setLoading(true)

    try {
      const response = await api.get(url, { responseType: 'blob' })
      const blob     = new Blob([response.data], {
        type: response.headers['content-type'] ?? 'application/octet-stream'
      })
      setContentType(response.headers['content-type'] ?? '')
      setObjectUrl(URL.createObjectURL(blob))
    } catch {
      setPreviewOpen(false)
    } finally {
      setLoading(false)
    }
  }

  async function handleDownload() {
    setDropdownOpen(false)

    try {
      const response = await api.get(url, { responseType: 'blob' })
      const blob     = new Blob([response.data])
      const link     = document.createElement('a')
      link.href      = URL.createObjectURL(blob)
      link.download  = fileName
      link.click()
      URL.revokeObjectURL(link.href)
    } catch {
      // silencioso
    }
  }

  function zoomIn()  { setZoom(z => Math.min(z + ZOOM_STEP, MAX_ZOOM)) }
  function zoomOut() { setZoom(z => Math.max(z - ZOOM_STEP, MIN_ZOOM)) }

  const isImageContent = contentType.startsWith('image/')
  const isPdfContent   = contentType === 'application/pdf'

  return (
    <>
      {/* Trigger + Dropdown */}
      <div ref={dropdownRef} className="relative inline-flex">
        <button
          ref={triggerRef}
          onClick={handleToggleDropdown}
          className="p-1 text-slate-500 hover:text-indigo-400 transition-colors"
          title="Ver comprovante"
        >
          {triggerIcon === 'paperclip'
            ? <Paperclip size={12} />
            : <FileText size={16} className="text-indigo-400" />
          }
        </button>

        {dropdownOpen && (
          <div className={`absolute z-50 left-1/2 -translate-x-1/2 w-36 bg-slate-800 border border-slate-700 rounded-xl shadow-xl overflow-hidden ${
            openUpward ? 'bottom-full mb-1' : 'top-full mt-1'
          }`}>
            <button
              onClick={handleVisualize}
              className="w-full flex items-center gap-2 px-3 py-2.5 text-sm text-slate-300 hover:bg-slate-700 hover:text-slate-100 transition-colors"
            >
              <Eye size={14} className="text-indigo-400 flex-shrink-0" />
              Visualizar
            </button>
            <div className="border-t border-slate-700" />
            <button
              onClick={handleDownload}
              className="w-full flex items-center gap-2 px-3 py-2.5 text-sm text-slate-300 hover:bg-slate-700 hover:text-slate-100 transition-colors"
            >
              <Download size={14} className="text-emerald-400 flex-shrink-0" />
              Baixar
            </button>
          </div>
        )}
      </div>

      {/* Modal de preview */}
      {previewOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-3xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">

            {/* Header */}
            <div className="flex items-center justify-between px-5 py-3 border-b border-slate-800 flex-shrink-0">
              <div className="flex items-center gap-2 min-w-0">
                <FileText size={16} className="text-indigo-400 flex-shrink-0" />
                <span className="text-sm text-slate-300 truncate">{fileName}</span>
              </div>
              <div className="flex items-center gap-1 flex-shrink-0">
                {/* Controles de zoom — só para imagens */}
                {!loading && objectUrl && isImageContent && (
                  <div className="flex items-center gap-1 mr-2">
                    <button
                      onClick={zoomOut}
                      disabled={zoom <= MIN_ZOOM}
                      className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                      title="Diminuir zoom"
                    >
                      <ZoomOut size={14} />
                    </button>
                    <span className="text-xs text-slate-400 w-10 text-center">
                      {zoom}%
                    </span>
                    <button
                      onClick={zoomIn}
                      disabled={zoom >= MAX_ZOOM}
                      className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                      title="Aumentar zoom"
                    >
                      <ZoomIn size={14} />
                    </button>
                    <div className="w-px h-4 bg-slate-700 mx-1" />
                  </div>
                )}
                <button
                  onClick={handleDownload}
                  className="flex items-center gap-1.5 text-xs text-slate-400 hover:text-slate-200 px-3 py-1.5 rounded-lg hover:bg-slate-800 transition-colors"
                >
                  <Download size={13} />
                  Baixar
                </button>
                <button
                  onClick={() => setPreviewOpen(false)}
                  aria-label="Fechar preview"
                  className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded-lg transition-colors"
                >
                  <X size={16} />
                </button>
              </div>
            </div>

            {/* Preview — com scroll */}
            <div className="flex-1 overflow-auto bg-slate-950">
              {loading && (
                <div className="flex items-center justify-center h-64">
                  <Loader2 size={24} className="animate-spin text-indigo-400" />
                </div>
              )}

              {!loading && objectUrl && isImageContent && (
                <div className="flex items-start justify-center p-4 min-h-full">
                  <img
                    src={objectUrl}
                    alt={fileName}
                    style={{ transform: `scale(${zoom / 100})`, transformOrigin: 'top center' }}
                    className="max-w-full transition-transform duration-200"
                  />
                </div>
              )}

              {!loading && objectUrl && isPdfContent && (
                <iframe
                  src={objectUrl}
                  title={fileName}
                  className="w-full h-full min-h-[70vh]"
                />
              )}

              {!loading && objectUrl && !isImageContent && !isPdfContent && (
                <div className="flex flex-col items-center gap-3 text-slate-500 py-12">
                  <FileText size={40} />
                  <p className="text-sm">Pré-visualização não disponível para este tipo de ficheiro.</p>
                  <button
                    onClick={handleDownload}
                    className="flex items-center gap-2 text-sm text-indigo-400 hover:text-indigo-300 transition-colors"
                  >
                    <Download size={14} />
                    Baixar ficheiro
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </>
  )
}