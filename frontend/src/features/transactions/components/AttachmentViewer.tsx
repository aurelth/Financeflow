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

  useEffect(() => {
    if (!previewOpen && objectUrl) {
      URL.revokeObjectURL(objectUrl)
      setObjectUrl(null)
      setZoom(100)
    }
  }, [previewOpen])

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node))
        setDropdownOpen(false)
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
      const blob     = new Blob([response.data], { type: response.headers['content-type'] ?? 'application/octet-stream' })
      setContentType(response.headers['content-type'] ?? '')
      setObjectUrl(URL.createObjectURL(blob))
    } catch { setPreviewOpen(false) }
    finally { setLoading(false) }
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
    } catch {}
  }

  const isImageContent = contentType.startsWith('image/')
  const isPdfContent   = contentType === 'application/pdf'

  return (
    <>
      <div ref={dropdownRef} className="relative inline-flex">
        <button
          ref={triggerRef}
          onClick={handleToggleDropdown}
          className="p-1 transition-colors"
          style={{ color: 'var(--ff-text-muted)' }}
          onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
          onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
          title="Ver comprovante"
        >
          {triggerIcon === 'paperclip'
            ? <Paperclip size={12} />
            : <FileText size={16} style={{ color: 'var(--ff-emerald)' }} />
          }
        </button>

        {dropdownOpen && (
          <div
            className={`absolute z-50 left-1/2 -translate-x-1/2 w-36 rounded-xl shadow-xl overflow-hidden ${openUpward ? 'bottom-full mb-1' : 'top-full mt-1'}`}
            style={{ background: 'var(--ff-bg-elevated)', border: '1px solid var(--ff-border)' }}
          >
            <button
              onClick={handleVisualize}
              className="w-full flex items-center gap-2 px-3 py-2.5 text-sm transition-colors"
              style={{ color: 'var(--ff-text-secondary)' }}
              onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-card)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
            >
              <Eye size={14} style={{ color: 'var(--ff-emerald)', flexShrink: 0 }} />
              Visualizar
            </button>
            <div style={{ borderTop: '1px solid var(--ff-border)' }} />
            <button
              onClick={handleDownload}
              className="w-full flex items-center gap-2 px-3 py-2.5 text-sm transition-colors"
              style={{ color: 'var(--ff-text-secondary)' }}
              onMouseEnter={e => (e.currentTarget.style.background = 'var(--ff-bg-card)')}
              onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
            >
              <Download size={14} style={{ color: 'var(--ff-income)', flexShrink: 0 }} />
              Baixar
            </button>
          </div>
        )}
      </div>

      {/* Modal de preview */}
      {previewOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
          <div
            className="w-full max-w-3xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh] rounded-2xl"
            style={{ background: 'var(--ff-bg-card)', border: '1px solid var(--ff-border)' }}
          >
            <div
              className="flex items-center justify-between px-5 py-3 flex-shrink-0"
              style={{ borderBottom: '1px solid var(--ff-border)' }}
            >
              <div className="flex items-center gap-2 min-w-0">
                <FileText size={16} style={{ color: 'var(--ff-emerald)', flexShrink: 0 }} />
                <span className="text-sm truncate" style={{ color: 'var(--ff-text-secondary)' }}>
                  {fileName}
                </span>
              </div>
              <div className="flex items-center gap-1 flex-shrink-0">
                {!loading && objectUrl && isImageContent && (
                  <div className="flex items-center gap-1 mr-2">
                    <button
                      onClick={() => setZoom(z => Math.max(z - ZOOM_STEP, MIN_ZOOM))}
                      disabled={zoom <= MIN_ZOOM}
                      className="p-1.5 rounded-lg transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                      style={{ color: 'var(--ff-text-muted)' }}
                      onMouseEnter={e => { if (zoom > MIN_ZOOM) e.currentTarget.style.color = 'var(--ff-text-primary)' }}
                      onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                      title="Diminuir zoom"
                    >
                      <ZoomOut size={14} />
                    </button>
                    <span className="text-xs w-10 text-center" style={{ color: 'var(--ff-text-muted)' }}>{zoom}%</span>
                    <button
                      onClick={() => setZoom(z => Math.min(z + ZOOM_STEP, MAX_ZOOM))}
                      disabled={zoom >= MAX_ZOOM}
                      className="p-1.5 rounded-lg transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                      style={{ color: 'var(--ff-text-muted)' }}
                      onMouseEnter={e => { if (zoom < MAX_ZOOM) e.currentTarget.style.color = 'var(--ff-text-primary)' }}
                      onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-text-muted)')}
                      title="Aumentar zoom"
                    >
                      <ZoomIn size={14} />
                    </button>
                    <div className="w-px h-4 mx-1" style={{ background: 'var(--ff-border)' }} />
                  </div>
                )}
                <button
                  onClick={handleDownload}
                  className="flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-lg transition-colors"
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
                  <Download size={13} />
                  Baixar
                </button>
                <button
                  onClick={() => setPreviewOpen(false)}
                  aria-label="Fechar preview"
                  className="p-1.5 rounded-lg transition-colors"
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
                  <X size={16} />
                </button>
              </div>
            </div>

            <div className="flex-1 overflow-auto" style={{ background: 'var(--ff-bg-base)' }}>
              {loading && (
                <div className="flex items-center justify-center h-64">
                  <Loader2 size={24} className="animate-spin" style={{ color: 'var(--ff-emerald)' }} />
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
                <iframe src={objectUrl} title={fileName} className="w-full h-full min-h-[70vh]" />
              )}
              {!loading && objectUrl && !isImageContent && !isPdfContent && (
                <div className="flex flex-col items-center gap-3 py-12" style={{ color: 'var(--ff-text-muted)' }}>
                  <FileText size={40} />
                  <p className="text-sm">Pré-visualização não disponível para este tipo de arquivo.</p>
                  <button
                    onClick={handleDownload}
                    className="flex items-center gap-2 text-sm transition-colors"
                    style={{ color: 'var(--ff-emerald)' }}
                    onMouseEnter={e => (e.currentTarget.style.color = 'var(--ff-emerald-hover)')}
                    onMouseLeave={e => (e.currentTarget.style.color = 'var(--ff-emerald)')}
                  >
                    <Download size={14} />
                    Baixar arquivo
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